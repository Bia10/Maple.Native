using System.Buffers;
using System.Buffers.Binary;
using System.IO;

namespace Maple.Native;

/// <summary>
/// Layout of the <c>ZXString&lt;char&gt;</c> object itself.
/// </summary>
public readonly ref struct ZXStringLayout
{
    /// <summary>Byte offset of <c>_m_pStr</c>.</summary>
    public const int StringPointerOffset = 0;

    /// <summary>Total object size in bytes (one payload pointer).</summary>
    public const int TotalBytes = TypeSizes.Pointer;
}

/// <summary>
/// Mirrors <c>ZXString&lt;char&gt;::_ZXStringData</c>:
/// <code>
/// struct _ZXStringData {
///     int nRef;       // +0x00  reference count
///     int nCap;       // +0x04  allocated capacity
///     int nByteLen;   // +0x08  payload byte length (excluding null terminator)
/// };
/// // immediately followed by char[] payload + null terminator
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// <c>ZXString&lt;T&gt;._m_pStr</c> points past the header, directly at the payload.
/// The header lives at <c>_m_pStr - sizeof(_ZXStringData)</c>.
/// </para>
/// <para>
/// Wide variant <c>ZXString&lt;unsigned short&gt;::_ZXStringData</c> (line 56548)
/// has identical layout; <c>nByteLen</c> stores byte length regardless of character width.
/// </para>
/// </remarks>
/// <param name="payloadBytes">Payload byte length (excluding the null terminator); used to compute <see cref="TotalBytes"/>.</param>
public readonly ref struct ZXStringDataLayout(int payloadBytes)
{
    /// <summary>Byte offset of <c>nRef</c> within the header.</summary>
    public const int RefCountOffset = 0;

    /// <summary>Byte offset of <c>nCap</c> within the header.</summary>
    public const int CapacityOffset = TypeSizes.Int32;

    /// <summary>Byte offset of <c>nByteLen</c> within the header.</summary>
    public const int ByteLengthOffset = TypeSizes.Int32 * 2;

    /// <summary>Total header size in bytes (3 × int32 = 12).</summary>
    public const int HeaderBytes = TypeSizes.Int32 * 3;

    /// <summary>Byte offset where the character payload begins (same as <see cref="HeaderBytes"/>).</summary>
    public const int PayloadOffset = HeaderBytes;

    /// <summary>Size of the null terminator following the payload.</summary>
    public const int NullTerminatorBytes = 1;

    private readonly int _payloadBytes = payloadBytes;

    /// <summary>Total allocation size: header + payload + null terminator.</summary>
    public int TotalBytes => HeaderBytes + _payloadBytes + NullTerminatorBytes;
}

/// <summary>
/// Mirrors <c>ZXString&lt;char&gt;</c>:
/// <code>
/// struct ZXString&lt;char&gt; {
///     char *_m_pStr;   // +0x00  → points at payload (past _ZXStringData header)
/// };
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The underlying memory starts with a <see cref="ZXStringDataLayout"/> header
/// (nRef, nCap, nByteLen), immediately followed by the character payload and a
/// null terminator. <c>_m_pStr</c> points at the payload, not the allocation base.
/// </para>
/// <para>
/// This C# representation stores the decoded <see cref="string"/> value directly —
/// the header fields are available for debugging via separate read methods.
/// </para>
/// </remarks>
public readonly struct ZXString
{
    private const int StackBufferBytes = 256;

    /// <summary>
    /// Addresses of one allocator-backed native <c>ZXString&lt;char&gt;</c> object graph.
    /// </summary>
    public readonly record struct Allocation(
        uint ObjectAddress,
        uint DataAddress,
        uint PayloadAddress,
        int ByteLength,
        int Capacity
    );

    /// <summary>Decoded string payload.</summary>
    public string Value { get; }

    /// <summary>Reference count from the <c>_ZXStringData</c> header.</summary>
    public int RefCount { get; }

    /// <summary>Allocated capacity from the <c>_ZXStringData</c> header.</summary>
    public int Capacity { get; }

    /// <summary>Byte length from the <c>_ZXStringData</c> header.</summary>
    public int ByteLength { get; }

    /// <summary>Creates a <see cref="ZXString"/> with the specified value and optional header metadata.</summary>
    /// <param name="value">Decoded string payload; must not be <see langword="null"/>.</param>
    /// <param name="refCount">Reference count from the <c>_ZXStringData</c> header; defaults to 1.</param>
    /// <param name="capacity">Allocated capacity; defaults to <c>value.Length</c> when zero.</param>
    /// <param name="byteLength">Byte length from <c>nByteLen</c>; defaults to <c>value.Length</c> when zero.</param>
    public ZXString(string value, int refCount = 1, int capacity = 0, int byteLength = 0)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
        RefCount = refCount;
        Capacity = capacity > 0 ? capacity : value.Length;
        ByteLength = byteLength > 0 ? byteLength : value.Length;
    }

    /// <summary>
    /// Reads a <c>ZXString&lt;char&gt;</c> from binary data at the given
    /// pointer (<c>_m_pStr</c>) file offset.
    /// </summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="payloadFileOffset">
    ///   File offset of <c>_m_pStr</c> (the payload, not the allocation base).
    /// </param>
    public static ZXString ReadFrom(ReadOnlySpan<byte> image, int payloadFileOffset)
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
            throw new InvalidDataException("ZXString byte length must be non-negative.");

        if (payloadFileOffset > image.Length - byteLen)
            throw new InvalidDataException("ZXString payload exceeds the readable image span.");

        string payload = System.Text.Encoding.Latin1.GetString(image.Slice(payloadFileOffset, byteLen));

        return new ZXString(payload, refCount, capacity, byteLen);
    }

    /// <summary>
    /// Allocates a native <c>ZXString&lt;char&gt;</c> object and backing payload block.
    /// </summary>
    /// <returns>The x86 address of the 4-byte <c>ZXString&lt;char&gt;</c> object.</returns>
    public static uint Create(INativeAllocator allocator, string value, int refCount = 1, int capacity = 0) =>
        Allocate(allocator, value, refCount, capacity).ObjectAddress;

    /// <summary>
    /// Allocates a native <c>ZXString&lt;char&gt;</c> object and returns all relevant addresses.
    /// </summary>
    public static Allocation Allocate(INativeAllocator allocator, string value, int refCount = 1, int capacity = 0)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentNullException.ThrowIfNull(value);

        int payloadLength = value.Length;
        int effectiveCapacity = capacity > 0 ? capacity : payloadLength;

        return payloadLength <= StackBufferBytes
            ? AllocateFromStringStack(allocator, value, refCount, effectiveCapacity)
            : AllocateFromStringPooled(allocator, value, refCount, effectiveCapacity);
    }

    /// <summary>
    /// Allocates a native <c>ZXString&lt;char&gt;</c> object from raw byte payload.
    /// </summary>
    /// <returns>The x86 address of the 4-byte <c>ZXString&lt;char&gt;</c> object.</returns>
    public static uint Create(
        INativeAllocator allocator,
        ReadOnlySpan<byte> payload,
        int refCount = 1,
        int capacity = 0
    ) => Allocate(allocator, payload, refCount, capacity).ObjectAddress;

    /// <summary>
    /// Allocates a native <c>ZXString&lt;char&gt;</c> object from raw byte payload and returns all relevant addresses.
    /// </summary>
    public static Allocation Allocate(
        INativeAllocator allocator,
        ReadOnlySpan<byte> payload,
        int refCount = 1,
        int capacity = 0
    )
    {
        ArgumentNullException.ThrowIfNull(allocator);

        return payload.Length <= StackBufferBytes
            ? AllocateFromBytesStack(allocator, payload, refCount, capacity > 0 ? capacity : payload.Length)
            : AllocateFromBytesPooled(allocator, payload, refCount, capacity > 0 ? capacity : payload.Length);
    }

    /// <summary>
    /// Releases a native <c>ZXString&lt;char&gt;</c> object and its backing allocation.
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

    private static Allocation AllocateFromStringStack(
        INativeAllocator allocator,
        string value,
        int refCount,
        int capacity
    )
    {
        var layout = new ZXStringDataLayout(value.Length);
        Span<byte> buffer = stackalloc byte[StackBufferBytes];
        Span<byte> data = buffer[..layout.TotalBytes];
        data.Clear();
        FillHeader(data, refCount, capacity, value.Length);
        WriteLatin1Payload(value, data[ZXStringDataLayout.PayloadOffset..]);
        return WriteAllocation(allocator, data, value.Length, capacity);
    }

    private static Allocation AllocateFromStringPooled(
        INativeAllocator allocator,
        string value,
        int refCount,
        int capacity
    )
    {
        var layout = new ZXStringDataLayout(value.Length);
        byte[] rented = ArrayPool<byte>.Shared.Rent(layout.TotalBytes);
        try
        {
            Span<byte> data = rented.AsSpan(0, layout.TotalBytes);
            data.Clear();
            FillHeader(data, refCount, capacity, value.Length);
            WriteLatin1Payload(value, data[ZXStringDataLayout.PayloadOffset..]);
            return WriteAllocation(allocator, data, value.Length, capacity);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    private static Allocation AllocateFromBytesStack(
        INativeAllocator allocator,
        ReadOnlySpan<byte> payload,
        int refCount,
        int capacity
    )
    {
        var layout = new ZXStringDataLayout(payload.Length);
        Span<byte> buffer = stackalloc byte[StackBufferBytes];
        Span<byte> data = buffer[..layout.TotalBytes];
        data.Clear();
        FillHeader(data, refCount, capacity, payload.Length);
        payload.CopyTo(data[ZXStringDataLayout.PayloadOffset..]);
        return WriteAllocation(allocator, data, payload.Length, capacity);
    }

    private static Allocation AllocateFromBytesPooled(
        INativeAllocator allocator,
        ReadOnlySpan<byte> payload,
        int refCount,
        int capacity
    )
    {
        var layout = new ZXStringDataLayout(payload.Length);
        byte[] rented = ArrayPool<byte>.Shared.Rent(layout.TotalBytes);
        try
        {
            Span<byte> data = rented.AsSpan(0, layout.TotalBytes);
            data.Clear();
            FillHeader(data, refCount, capacity, payload.Length);
            payload.CopyTo(data[ZXStringDataLayout.PayloadOffset..]);
            return WriteAllocation(allocator, data, payload.Length, capacity);
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

    private static void WriteLatin1Payload(string value, Span<byte> destination)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c > byte.MaxValue)
                throw new ArgumentException(
                    "ZXString<char> can only be created losslessly from byte-sized characters.",
                    nameof(value)
                );

            destination[i] = (byte)c;
        }
    }

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
            throw new InvalidOperationException("Failed to write ZXString<char> backing data.");
        }

        Span<byte> objectBytes = stackalloc byte[ZXStringLayout.TotalBytes];
        uint payloadAddress = checked(dataAddress + (uint)ZXStringDataLayout.PayloadOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(objectBytes, payloadAddress);

        uint objectAddress = allocator.Allocate(objectBytes.Length);
        if (!allocator.Write(objectAddress, objectBytes))
        {
            allocator.Free(objectAddress);
            allocator.Free(dataAddress);
            throw new InvalidOperationException("Failed to write ZXString<char> object header.");
        }

        return new Allocation(objectAddress, dataAddress, payloadAddress, byteLength, capacity);
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Implicit conversion to <see cref="string"/> returning the decoded payload.</summary>
    public static implicit operator string(ZXString s) => s.Value;
}
