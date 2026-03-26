using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Mirrors <c>ZRefCounted</c>:
/// <code>
/// struct ZRefCounted {
///     ZRefCounted_vtbl *__vftable;  // +0x00  virtual function table pointer
///     union {
///         int           _m_nRef;    // +0x04  reference count (when live)
///         ZRefCounted  *_m_pNext;   // +0x04  free-list next pointer (when recycled)
///     };
///     ZRefCounted *_m_pPrev;        // +0x08  previous node in allocator list
/// };  // sizeof = 0x0C
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Base class for all reference-counted heap objects in the MapleStory engine.
/// </para>
/// <para>
/// The <c>_m_nRef</c>/<c>_m_pNext</c> union shares a 4-byte slot: when the object
/// is live <c>_m_nRef</c> is the reference count; when recycled by <c>ZRecyclable</c>
/// it becomes the free-list next pointer.
/// </para>
/// </remarks>
public readonly ref struct ZRefCountedLayout
{
    /// <summary>Byte offset of the virtual function table pointer.</summary>
    public const int VTableOffset = 0;

    /// <summary>
    /// Byte offset of <c>_m_nRef</c> — the reference count when the object is live,
    /// also aliased as <c>_m_pNext</c> when the object is on the free list.
    /// </summary>
    public const int RefCountOffset = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>_m_pPrev</c> (previous node in allocator list).</summary>
    public const int PrevOffset = TypeSizes.Pointer * 2;

    /// <summary>Total struct size in bytes (3 × pointer = 12).</summary>
    public const int TotalBytes = TypeSizes.Pointer * 3;
}

/// <summary>
/// C# representation of a live <c>ZRefCounted</c> header.
/// </summary>
/// <param name="vTablePointer">Runtime vtable pointer.</param>
/// <param name="refCount">Reference count (<c>_m_nRef</c>) — valid when the object is live.</param>
/// <param name="prevPointer">Runtime <c>_m_pPrev</c> allocator-list pointer.</param>
public readonly struct ZRefCounted(uint vTablePointer, int refCount, uint prevPointer) : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => ZRefCountedLayout.TotalBytes;

    /// <summary>Runtime vtable pointer.</summary>
    public uint VTablePointer { get; } = vTablePointer;

    /// <summary>Reference count — number of live <c>ZRef</c> owners.</summary>
    public int RefCount { get; } = refCount;

    /// <summary>Runtime <c>_m_pPrev</c> allocator-list pointer.</summary>
    public uint PrevPointer { get; } = prevPointer;

    /// <summary>
    /// Reads a <c>ZRefCounted</c> header from <paramref name="image"/> at <paramref name="fileOffset"/>.
    /// </summary>
    public static ZRefCounted ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZRefCountedLayout.VTableOffset)..]),
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + ZRefCountedLayout.RefCountOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZRefCountedLayout.PrevOffset)..])
        );
}
