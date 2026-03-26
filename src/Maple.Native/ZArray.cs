using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Maple.Native;

/// <summary>
/// Mirrors <c>ZArray&lt;T&gt;</c>:
/// <code>
/// struct ZArray&lt;T&gt; {
///     T *a;   // +0x00  → points past count header
/// };
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The allocation is: <c>[int32 count][T[0], T[1], … T[count-1]]</c>.
/// The <c>a</c> pointer points at <c>T[0]</c>, not at the count.
/// The count lives at <c>a - sizeof(int)</c>.
/// </para>
/// <para>
/// In the PDB this is instantiated as:
/// <list type="bullet">
///   <item><c>ZArray&lt;ZXString&lt;char&gt; *&gt;</c> — <c>a</c> is <c>ZXString&lt;char&gt;**</c></item>
///   <item><c>ZArray&lt;ZXString&lt;unsigned short&gt; *&gt;</c></item>
///   <item><c>ZArray&lt;unsigned char&gt;</c> — <c>a</c> is <c>unsigned char*</c></item>
///   <item><c>ZArray&lt;long&gt;</c> — <c>a</c> is <c>int*</c></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="elementCount">Number of elements in this array instance.</param>
public readonly ref struct ZArrayLayout(int elementCount)
{
    /// <summary>Offset of the element count field relative to the allocation base.</summary>
    public const int CountOffset = 0;

    /// <summary>Header size in bytes (one int32 count).</summary>
    public const int HeaderBytes = TypeSizes.Int32;

    /// <summary>Byte offset where array elements begin.</summary>
    public const int PayloadOffset = HeaderBytes;

    /// <summary>Number of elements in the array.</summary>
    public int ElementCount { get; } = elementCount;

    /// <summary>Total allocation size: header plus all elements of the given size.</summary>
    public int TotalBytes(int elementSize) => HeaderBytes + (ElementCount * elementSize);
}

/// <summary>
/// Typed reader for <c>ZArray&lt;T&gt;</c> elements from a binary image.
/// </summary>
public static class ZArray
{
    /// <summary>
    /// Reads the element count from the allocation header.
    /// <c>a</c> points at the payload; count is at <c>a - 4</c>.
    /// </summary>
    public static int ReadCount(ReadOnlySpan<byte> image, int payloadFileOffset)
    {
        if (payloadFileOffset < ZArrayLayout.HeaderBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadFileOffset),
                payloadFileOffset,
                $"Payload offset must be at least {ZArrayLayout.HeaderBytes} so the count header is addressable."
            );
        }

        int countOffset = payloadFileOffset - ZArrayLayout.HeaderBytes;
        if (countOffset > image.Length - ZArrayLayout.HeaderBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadFileOffset),
                payloadFileOffset,
                "Payload offset exceeds the readable image range for a ZArray count header."
            );
        }

        return BinaryPrimitives.ReadInt32LittleEndian(image[countOffset..]);
    }

    /// <summary>
    /// Reads all <c>uint32</c> pointer elements from a <c>ZArray&lt;T*&gt;</c>.
    /// </summary>
    public static uint[] ReadPointerElements(ReadOnlySpan<byte> image, int payloadFileOffset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadFileOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        int totalBytes = checked(count * TypeSizes.Pointer);
        if (payloadFileOffset > image.Length - totalBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payloadFileOffset),
                payloadFileOffset,
                "Pointer element range exceeds the readable image span."
            );
        }

        var result = new uint[count];
        for (int i = 0; i < count; i++)
        {
            int elementOffset = payloadFileOffset + (i * TypeSizes.Pointer);
            result[i] = BinaryPrimitives.ReadUInt32LittleEndian(image[elementOffset..]);
        }
        return result;
    }

    /// <summary>
    /// Reads all byte elements from a <c>ZArray&lt;unsigned char&gt;</c>.
    /// </summary>
    public static byte[] ReadByteElements(ReadOnlySpan<byte> image, int payloadFileOffset, int count) =>
        image.Slice(payloadFileOffset, count).ToArray();
}

/// <summary>
/// Writer for native <c>ZArray&lt;T&gt;</c> allocations.
/// </summary>
public static class ZArray<T>
    where T : unmanaged
{
    private const int StackBufferBytes = 256;

    /// <summary>
    /// Describes one native <c>ZArray&lt;T&gt;</c> allocation.
    /// </summary>
    public readonly record struct Allocation(uint BaseAddress, uint PayloadAddress, int ElementCount);

    /// <summary>
    /// Allocates a native <c>ZArray&lt;T&gt;</c> backing store and returns both base and payload addresses.
    /// </summary>
    public static Allocation Allocate(INativeAllocator allocator, ReadOnlySpan<T> elements)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        int elementBytes = Unsafe.SizeOf<T>();
        int totalBytes = checked(ZArrayLayout.HeaderBytes + (elements.Length * elementBytes));
        uint baseAddress = allocator.Allocate(totalBytes);

        WriteAllocation(allocator, baseAddress, elements, totalBytes);

        return new Allocation(baseAddress, checked(baseAddress + (uint)ZArrayLayout.PayloadOffset), elements.Length);
    }

    /// <summary>
    /// Allocates a native <c>ZArray&lt;T&gt;</c> backing store and returns the payload pointer stored in <c>a</c>.
    /// </summary>
    /// <remarks>
    /// The returned address points at the first element, not at the count header.
    /// The element count is written at <c>address - 4</c> to match Maple's native layout.
    /// </remarks>
    public static uint Create(INativeAllocator allocator, ReadOnlySpan<T> elements) =>
        Allocate(allocator, elements).PayloadAddress;

    /// <summary>
    /// Releases a native <c>ZArray&lt;T&gt;</c> allocation when given the payload pointer stored in <c>a</c>.
    /// </summary>
    public static void Destroy(INativeAllocator allocator, uint payloadAddress)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        if (payloadAddress < ZArrayLayout.PayloadOffset)
            throw new ArgumentOutOfRangeException(nameof(payloadAddress));

        allocator.Free(payloadAddress - ZArrayLayout.PayloadOffset);
    }

    private static void WriteAllocation(
        INativeAllocator allocator,
        uint baseAddress,
        ReadOnlySpan<T> elements,
        int totalBytes
    )
    {
        if (totalBytes <= StackBufferBytes)
        {
            Span<byte> stackBuffer = stackalloc byte[StackBufferBytes];
            Span<byte> slice = stackBuffer[..totalBytes];
            slice.Clear();
            FillBuffer(slice, elements);
            if (!allocator.Write(baseAddress, slice))
            {
                allocator.Free(baseAddress);
                throw new InvalidOperationException("Failed to write the ZArray allocation.");
            }

            return;
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent(totalBytes);
        try
        {
            Span<byte> slice = rented.AsSpan(0, totalBytes);
            slice.Clear();
            FillBuffer(slice, elements);
            if (!allocator.Write(baseAddress, slice))
            {
                allocator.Free(baseAddress);
                throw new InvalidOperationException("Failed to write the ZArray allocation.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    private static void FillBuffer(Span<byte> buffer, ReadOnlySpan<T> elements)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer, elements.Length);
        MemoryMarshal.AsBytes(elements).CopyTo(buffer[ZArrayLayout.PayloadOffset..]);
    }
}
