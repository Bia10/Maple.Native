using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Maple.Native;

/// <summary>
/// Mirrors <c>ZXString&lt;unsigned short&gt;</c>:
/// <code>
/// struct ZXString&lt;unsigned short&gt; {
///     wchar_t *_m_pStr;   // +0x00  → points at payload (past _ZXStringData header)
/// };
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The header layout is identical to the narrow <see cref="ZXString"/> variant:
/// a 12-byte <c>_ZXStringData</c> block (nRef, nCap, nByteLen) lives immediately
/// before the payload. <c>_m_pStr</c> points at the payload, not the allocation base.
/// </para>
/// <para>
/// <c>nByteLen</c> stores byte length — divide by 2 to get the character count.
/// The null terminator is <c>L'\0'</c> (2 bytes: <c>0x00 0x00</c>).
/// </para>
/// <para>
/// PDB source: line 19604 (<c>ZXString&lt;unsigned short&gt;</c>).
/// Header offsets are shared with <see cref="ZXStringDataLayout"/>.
/// </para>
/// </remarks>
public readonly struct ZXStringWide
{
    private const int StackBufferBytes = 512;

    /// <summary>
    /// Addresses of one allocator-backed native <c>ZXString&lt;unsigned short&gt;</c> object graph.
    /// </summary>
    public readonly record struct Allocation(
        uint ObjectAddress,
        uint DataAddress,
        uint PayloadAddress,
        int ByteLength,
        int Capacity
    );

    /// <summary>Decoded wide string payload.</summary>
    public string Value { get; }

    /// <summary>Reference count from the <c>_ZXStringData</c> header.</summary>
    public int RefCount { get; }

    /// <summary>Allocated capacity from the <c>_ZXStringData</c> header (in <c>wchar_t</c> units).</summary>
    public int Capacity { get; }

    /// <summary>Byte length from <c>nByteLen</c> — byte count, not character count.</summary>
    public int ByteLength { get; }

    /// <summary>Character count — <see cref="ByteLength"/> divided by 2.</summary>
    public int CharCount => ByteLength / 2;

    /// <summary>
    /// Size of the wide null terminator (<c>L'\0' = 0x0000</c>) in bytes.
    /// </summary>
    public const int NullTerminatorBytes = 2;

    /// <summary>Creates a <see cref="ZXStringWide"/> with the specified value and optional header metadata.</summary>
    /// <param name="value">Decoded string payload; must not be <see langword="null"/>.</param>
    /// <param name="refCount">Reference count from the <c>_ZXStringData</c> header; defaults to 1.</param>
    /// <param name="capacity">Allocated capacity (in <c>wchar_t</c> units); defaults to <c>value.Length</c> when zero.</param>
    /// <param name="byteLength">Byte length from <c>nByteLen</c>; defaults to <c>value.Length * 2</c> when zero.</param>
    public ZXStringWide(string value, int refCount = 1, int capacity = 0, int byteLength = 0)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
        RefCount = refCount;
        Capacity = capacity > 0 ? capacity : value.Length;
        ByteLength = byteLength > 0 ? byteLength : value.Length * 2;
    }

    /// <summary>
    /// Reads a <c>ZXString&lt;unsigned short&gt;</c> from binary data at the given
    /// pointer (<c>_m_pStr</c>) file offset.
    /// </summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="payloadFileOffset">
    ///   File offset of <c>_m_pStr</c> (the payload, not the allocation base).
    /// </param>
    public static ZXStringWide ReadFrom(ReadOnlySpan<byte> image, int payloadFileOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadFileOffset);
        if (payloadFileOffset < ZXStringDataLayout.HeaderBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadFileOffset),
                payloadFileOffset,
                $"Payload offset must be at least {ZXStringDataLayout.HeaderBytes} so the ZXString header is addressable."
            );
        }

        int headerBase = payloadFileOffset - ZXStringDataLayout.HeaderBytes;
        if (headerBase > image.Length - ZXStringDataLayout.HeaderBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadFileOffset),
                payloadFileOffset,
                "Payload offset exceeds the readable image range for a ZXString header."
            );
        }

        int refCount = BinaryPrimitives.ReadInt32LittleEndian(
            image[(headerBase + ZXStringDataLayout.RefCountOffset)..]
        );
        int capacity = BinaryPrimitives.ReadInt32LittleEndian(
            image[(headerBase + ZXStringDataLayout.CapacityOffset)..]
        );
        int byteLen = BinaryPrimitives.ReadInt32LittleEndian(
            image[(headerBase + ZXStringDataLayout.ByteLengthOffset)..]
        );
        if (byteLen < 0)
            throw new InvalidDataException("ZXStringWide byte length must be non-negative.");

        if ((byteLen & 1) != 0)
            throw new InvalidDataException("ZXStringWide byte length must be even.");

        if (payloadFileOffset > image.Length - byteLen)
            throw new InvalidDataException("ZXStringWide payload exceeds the readable image span.");

        string payload = Encoding.Unicode.GetString(image.Slice(payloadFileOffset, byteLen));

        return new ZXStringWide(payload, refCount, capacity, byteLen);
    }

    /// <summary>
    /// Allocates a native <c>ZXString&lt;unsigned short&gt;</c> object and UTF-16 backing block.
    /// </summary>
    /// <returns>The x86 address of the 4-byte <c>ZXString&lt;unsigned short&gt;</c> object.</returns>
    public static uint Create(INativeAllocator allocator, string value, int refCount = 1, int capacity = 0) =>
        Allocate(allocator, value, refCount, capacity).ObjectAddress;

    /// <summary>
    /// Allocates a native <c>ZXString&lt;unsigned short&gt;</c> object and returns all relevant addresses.
    /// </summary>
    public static Allocation Allocate(INativeAllocator allocator, string value, int refCount = 1, int capacity = 0)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentNullException.ThrowIfNull(value);

        int payloadBytes = checked(value.Length * 2);
        int effectiveCapacity = capacity > 0 ? capacity : value.Length;
        int totalBytes = GetAllocationBytes(payloadBytes);

        return totalBytes <= StackBufferBytes
            ? AllocateStack(allocator, value, refCount, effectiveCapacity, totalBytes)
            : AllocatePooled(allocator, value, refCount, effectiveCapacity, totalBytes);
    }

    /// <summary>
    /// Releases a native <c>ZXString&lt;unsigned short&gt;</c> object and its backing allocation.
    /// </summary>
    public static void Destroy(INativeAllocator allocator, uint objectAddress)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        Span<byte> objectBytes = stackalloc byte[ZXStringLayout.TotalBytes];
        if (!allocator.Read(objectAddress, objectBytes))
            throw new ArgumentOutOfRangeException(
                nameof(objectAddress),
                $"Address 0x{objectAddress:X8} is not readable."
            );

        uint payloadAddress = BinaryPrimitives.ReadUInt32LittleEndian(objectBytes);
        if (payloadAddress >= ZXStringDataLayout.HeaderBytes)
            allocator.Free(payloadAddress - (uint)ZXStringDataLayout.HeaderBytes);

        allocator.Free(objectAddress);
    }

    private static Allocation AllocateStack(
        INativeAllocator allocator,
        string value,
        int refCount,
        int capacity,
        int totalBytes
    )
    {
        Span<byte> buffer = stackalloc byte[StackBufferBytes];
        Span<byte> data = buffer[..totalBytes];
        data.Clear();
        FillHeader(data, refCount, capacity, value.Length * 2);
        WritePayload(value, data[ZXStringDataLayout.PayloadOffset..]);
        return WriteAllocation(allocator, data, value.Length * 2, capacity);
    }

    private static Allocation AllocatePooled(
        INativeAllocator allocator,
        string value,
        int refCount,
        int capacity,
        int totalBytes
    )
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(totalBytes);
        try
        {
            Span<byte> data = rented.AsSpan(0, totalBytes);
            data.Clear();
            FillHeader(data, refCount, capacity, value.Length * 2);
            WritePayload(value, data[ZXStringDataLayout.PayloadOffset..]);
            return WriteAllocation(allocator, data, value.Length * 2, capacity);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    private static void FillHeader(Span<byte> data, int refCount, int capacity, int byteLength)
    {
        BinaryPrimitives.WriteInt32LittleEndian(data[ZXStringDataLayout.RefCountOffset..], refCount);
        BinaryPrimitives.WriteInt32LittleEndian(data[ZXStringDataLayout.CapacityOffset..], capacity);
        BinaryPrimitives.WriteInt32LittleEndian(data[ZXStringDataLayout.ByteLengthOffset..], byteLength);
    }

    private static void WritePayload(string value, Span<byte> destination)
    {
        int written = Encoding.Unicode.GetBytes(value.AsSpan(), destination);
        if (written != value.Length * 2)
            throw new InvalidOperationException("Failed to encode the full UTF-16 payload.");
    }

    private static int GetAllocationBytes(int payloadBytes) =>
        checked(ZXStringDataLayout.HeaderBytes + payloadBytes + NullTerminatorBytes);

    private static Allocation WriteAllocation(
        INativeAllocator allocator,
        ReadOnlySpan<byte> data,
        int byteLength,
        int capacity
    )
    {
        uint dataAddress = allocator.Allocate(data.Length);
        if (!allocator.Write(dataAddress, data))
        {
            allocator.Free(dataAddress);
            throw new InvalidOperationException("Failed to write ZXString<unsigned short> backing data.");
        }

        Span<byte> objectBytes = stackalloc byte[ZXStringLayout.TotalBytes];
        uint payloadAddress = checked(dataAddress + (uint)ZXStringDataLayout.PayloadOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(objectBytes, payloadAddress);

        uint objectAddress = allocator.Allocate(objectBytes.Length);
        if (!allocator.Write(objectAddress, objectBytes))
        {
            allocator.Free(objectAddress);
            allocator.Free(dataAddress);
            throw new InvalidOperationException("Failed to write ZXString<unsigned short> object header.");
        }

        return new Allocation(objectAddress, dataAddress, payloadAddress, byteLength, capacity);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Implicit conversion to <see cref="string"/> returning the decoded payload.</summary>
    public static implicit operator string(ZXStringWide s) => s.Value;
}
