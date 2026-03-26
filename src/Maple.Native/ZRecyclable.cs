using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>ZRecyclable&lt;T, N, T2&gt;</c> — the base class that marks a
/// type as recyclable by the Maple slab allocator and gives it a virtual destructor:
/// <code>
/// struct ZRecyclable&lt;T, N, T2&gt; : ZAllocBase  // ZAllocBase is empty — EBO applied
/// {
///     ZRecyclable_vtbl *__vftable;  // +0x00  virtual destructor table
/// };  // sizeof = 0x04
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// PDB representative: <c>ZRecyclable&lt;ZMap&lt;long,long,long&gt;::_PAIR,16,ZMap&lt;long,long,long&gt;::_PAIR&gt;</c>
/// line 21252 (ordinal 1826).
/// </para>
/// <para>
/// The template parameter <c>N</c> is fixed to 16 across all observed instantiations.
/// It controls the recycler's slab pool configuration and does <b>not</b> affect the
/// instance layout.
/// </para>
/// </remarks>
public readonly ref struct ZRecyclableLayout
{
    /// <summary>Byte offset of the virtual function table pointer.</summary>
    public const int VTableOffset = 0;

    /// <summary>Total struct size in bytes (one vtable pointer = 4).</summary>
    public const int TotalBytes = TypeSizes.Pointer;
}

/// <summary>
/// Memory layout of <c>ZRecyclableStatic</c> — a singly-linked list of
/// static-initializer callbacks:
/// <code>
/// struct ZRecyclableStatic
/// {
///     ZRecyclableStatic::CallBack *m_pHead;  // +0x00  first registered callback
/// };  // sizeof = 0x04
/// </code>
/// </summary>
/// <remarks>PDB source: <c>game_types.h</c> line 17703 (ordinal 1557).</remarks>
public readonly ref struct ZRecyclableStaticLayout
{
    /// <summary>Byte offset of <c>m_pHead</c> (first callback VA).</summary>
    public const int HeadOffset = 0;

    /// <summary>Total struct size in bytes (one pointer = 4).</summary>
    public const int TotalBytes = TypeSizes.Pointer;
}

/// <summary>
/// Memory layout of <c>ZRefCountedDummy&lt;T&gt;</c> — the heap node that wraps a
/// payload of type <c>T</c> inside a <c>ZRefCounted</c> + <c>ZRecyclable</c> header,
/// serving as the intrusive list node for <c>ZList&lt;T&gt;</c>:
/// <code>
/// struct ZRefCountedDummy&lt;T&gt; : ZRefCounted, ZRecyclable&lt;ZRefCountedDummy&lt;T&gt;, 16, T&gt;
/// {
///     // ── ZRefCounted base ──
///     ZRefCounted_vtbl *__vftable;    // +0x00  primary vtable (4 bytes)
///     ZRefCounted      *_m_pNext;     // +0x04  backward link toward tail in ZList (4 bytes)
///     ZRefCounted      *_m_pPrev;     // +0x08  forward link toward head  in ZList (4 bytes)
///     // ── ZRecyclable base (second vtable subobject) ──
///     ZRecyclable_vtbl *__vftable2;   // +0x0C  secondary vtable (4 bytes)
///     // ── Payload ──
///     T                 t;             // +0x10  inner value (sizeof(T) bytes)
/// };
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// PDB representative: <c>ZRefCountedDummy&lt;ZRef&lt;CCtrlWnd&gt;&gt;</c>
/// line 19639 (ordinal 1757).
/// </para>
/// <para>
/// <b>ZList traversal:</b> <c>ZList&lt;T&gt;._m_pHead</c> / <c>._m_pTail</c> store
/// <c>T*</c>, pointing directly at <see cref="DataOffset"/> (+0x10) within the node —
/// <b>not</b> at the ZRefCounted node base.
/// </para>
/// <para>
/// To walk forward from <c>_m_pHead</c> toward <c>_m_pTail</c>:
/// <list type="number">
///   <item>node_base_va = T_va − <see cref="DataOffset"/></item>
///   <item>next_va = read uint32 at (node_base_va + <see cref="PrevPointerOffset"/>)</item>
///   <item>If next_va == 0: end of list.</item>
///   <item>next_T_va = next_va + <see cref="DataOffset"/></item>
/// </list>
/// </para>
/// <para>
/// <b>Naming note:</b> <c>_m_pPrev</c> is the <em>forward</em> traversal link because
/// <c>_m_pHead</c> stores the most-recently-inserted element and <c>_m_pPrev</c>
/// connects to the previously-inserted (older) element.
/// </para>
/// </remarks>
public readonly ref struct ZRefCountedDummyLayout
{
    // ── ZRefCounted base ──────────────────────────────────────────────────────

    /// <summary>Byte offset of the primary vtable pointer (from <c>ZRefCounted</c>).</summary>
    public const int VTableOffset = 0;

    /// <summary>
    /// Byte offset of the <c>_m_nRef</c>/<c>_m_pNext</c> union.
    /// When the node lives in a <c>ZList</c> this holds the <b>backward</b> link
    /// (toward the tail / oldest-insertion end).
    /// </summary>
    public const int RefCountOrNextOffset = TypeSizes.Pointer;

    /// <summary>
    /// Byte offset of <c>_m_pPrev</c>.
    /// When the node lives in a <c>ZList</c> this is the <b>forward traversal link</b>
    /// (toward the head / most-recently-inserted end).
    /// Null for the tail element.
    /// </summary>
    public const int PrevPointerOffset = TypeSizes.Pointer * 2;

    // ── ZRecyclable base ──────────────────────────────────────────────────────

    /// <summary>Byte offset of the secondary vtable pointer (from <c>ZRecyclable</c>).</summary>
    public const int RecyclableVTableOffset = TypeSizes.Pointer * 3;

    // ── Payload ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Byte offset where the inner value <c>T t</c> begins (0x10 = 16).
    /// <c>ZList&lt;T&gt;._m_pHead</c> and <c>._m_pTail</c> point here,
    /// not to the node base.
    /// </summary>
    public const int DataOffset = TypeSizes.Pointer * 4;

    /// <summary>
    /// Total size of the fixed header (everything before the <c>T</c> payload).
    /// Equal to <see cref="DataOffset"/> = 16 bytes.
    /// </summary>
    public const int HeaderBytes = DataOffset;
}

/// <summary>
/// C# representation of a decoded <c>ZRefCountedDummy&lt;T&gt;</c> node header
/// (the fields before the <c>T</c> payload).
/// </summary>
/// <param name="vTablePointer">Runtime primary vtable pointer.</param>
/// <param name="nextPointer">
/// Runtime <c>_m_pNext</c> / <c>_m_nRef</c> union — backward link when in a ZList.
/// </param>
/// <param name="prevPointer">
/// Runtime <c>_m_pPrev</c> — forward traversal link when in a ZList.
/// </param>
/// <param name="recyclableVTablePointer">Runtime secondary (ZRecyclable) vtable pointer.</param>
public readonly struct ZRefCountedDummyHeader(
    uint vTablePointer,
    uint nextPointer,
    uint prevPointer,
    uint recyclableVTablePointer
)
{
    /// <summary>Runtime primary vtable pointer.</summary>
    public uint VTablePointer { get; } = vTablePointer;

    /// <summary>
    /// Runtime value of the <c>_m_nRef</c>/<c>_m_pNext</c> union.
    /// Interpret as a backward link (VA of next ZRefCounted node base) when the node
    /// is inside a <c>ZList</c>.
    /// </summary>
    public uint NextPointer { get; } = nextPointer;

    /// <summary>
    /// Runtime <c>_m_pPrev</c> — forward link toward head when inside a <c>ZList</c>.
    /// Zero indicates the tail element.
    /// </summary>
    public uint PrevPointer { get; } = prevPointer;

    /// <summary>Runtime secondary (ZRecyclable) vtable pointer.</summary>
    public uint RecyclableVTablePointer { get; } = recyclableVTablePointer;

    /// <summary>
    /// Reads a <c>ZRefCountedDummy</c> node header from <paramref name="image"/>
    /// at <paramref name="nodeBaseFileOffset"/> (the ZRefCounted base of the node,
    /// i.e. <c>T_fileOffset − <see cref="ZRefCountedDummyLayout.DataOffset"/></c>).
    /// </summary>
    public static ZRefCountedDummyHeader ReadFrom(ReadOnlySpan<byte> image, int nodeBaseFileOffset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(
                image[(nodeBaseFileOffset + ZRefCountedDummyLayout.VTableOffset)..]
            ),
            BinaryPrimitives.ReadUInt32LittleEndian(
                image[(nodeBaseFileOffset + ZRefCountedDummyLayout.RefCountOrNextOffset)..]
            ),
            BinaryPrimitives.ReadUInt32LittleEndian(
                image[(nodeBaseFileOffset + ZRefCountedDummyLayout.PrevPointerOffset)..]
            ),
            BinaryPrimitives.ReadUInt32LittleEndian(
                image[(nodeBaseFileOffset + ZRefCountedDummyLayout.RecyclableVTableOffset)..]
            )
        );
}
