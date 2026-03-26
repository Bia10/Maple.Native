using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Mirrors <c>ZList&lt;T&gt;</c>:
/// <code>
/// struct ZList&lt;T&gt; : ZRefCountedAccessor&lt;T&gt;, ZRefCountedAccessor&lt;ZRefCountedDummy&lt;T&gt;&gt;
/// {
///     ZList_vtbl   *__vftable;    // +0x00  (4 bytes)
///     _BYTE         gap4;         // +0x04  empty-base padding (1 byte + 3 alignment = 4 total)
///     unsigned int  _m_uCount;    // +0x08  element count
///     T            *_m_pHead;     // +0x0C  T* pointing to most-recently-inserted element
///     T            *_m_pTail;     // +0x10  T* pointing to the oldest element
/// };  // sizeof = 0x14
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Intrusive doubly-linked list backed by <c>ZRefCountedDummy&lt;T&gt;</c> nodes.
/// <c>_m_pHead</c> and <c>_m_pTail</c> store <c>T*</c> pointers that point directly
/// into the node at offset <see cref="ZRefCountedDummyLayout.DataOffset"/> (+0x10)
/// — <b>not</b> at the ZRefCounted base of the node.
/// </para>
/// <para>
/// To walk forward from <c>_m_pHead</c> toward <c>_m_pTail</c>:
/// <list type="number">
///   <item>Convert <c>T* VA</c> (from <see cref="HeadOffset"/> or previous iteration) to
///         a file offset.</item>
///   <item><c>node_base_offset = T_offset − <see cref="ZRefCountedDummyLayout.DataOffset"/></c></item>
///   <item>Read <c>uint32</c> at <c>node_base_offset + <see cref="ZRefCountedDummyLayout.PrevPointerOffset"/></c>
///         → <c>next_node_base_VA</c>.</item>
///   <item>If <c>next_node_base_VA == 0</c>: end of list.</item>
///   <item><c>next_T_VA = next_node_base_VA + <see cref="ZRefCountedDummyLayout.DataOffset"/></c></item>
/// </list>
/// </para>
/// </remarks>
public readonly ref struct ZListLayout
{
    /// <summary>Byte offset of the virtual function table pointer.</summary>
    public const int VTableOffset = 0;

    /// <summary>
    /// Byte offset of the empty-base padding gap
    /// (1 visible byte at +0x04 plus 3 implicit alignment bytes; 4 bytes total).
    /// </summary>
    public const int GapOffset = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>_m_uCount</c> (element count).</summary>
    public const int CountOffset = TypeSizes.Pointer * 2;

    /// <summary>Byte offset of <c>_m_pHead</c> (first element pointer).</summary>
    public const int HeadOffset = CountOffset + TypeSizes.Int32;

    /// <summary>Byte offset of <c>_m_pTail</c> (last element pointer).</summary>
    public const int TailOffset = HeadOffset + TypeSizes.Pointer;

    /// <summary>Total struct size in bytes (0x14 = 20).</summary>
    public const int TotalBytes = TailOffset + TypeSizes.Pointer;
}

/// <summary>
/// Typed reader for <c>ZList&lt;T&gt;</c> header fields from a binary image.
/// </summary>
public static class ZList
{
    /// <summary>
    /// Reads the element count from a <c>ZList&lt;T&gt;</c> at <paramref name="fileOffset"/>.
    /// </summary>
    public static uint ReadCount(ReadOnlySpan<byte> image, int fileOffset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZListLayout.CountOffset)..]);

    /// <summary>
    /// Reads the head pointer from a <c>ZList&lt;T&gt;</c> at <paramref name="fileOffset"/>.
    /// </summary>
    public static uint ReadHead(ReadOnlySpan<byte> image, int fileOffset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZListLayout.HeadOffset)..]);

    /// <summary>
    /// Reads the tail pointer from a <c>ZList&lt;T&gt;</c> at <paramref name="fileOffset"/>.
    /// </summary>
    public static uint ReadTail(ReadOnlySpan<byte> image, int fileOffset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZListLayout.TailOffset)..]);
}
