namespace Maple.Native;

/// <summary>
/// Empty CRTP base for all Maple allocator families:
/// <code>
/// struct ZAllocBase {};  // sizeof = 0
/// </code>
/// </summary>
/// <remarks>
/// PDB source: <c>game_types.h</c> line 17660 (ordinal 1549).
/// Zero-byte empty base — contributes nothing to derived struct layouts.
/// </remarks>
public readonly ref struct ZAllocBaseLayout
{
    /// <summary>Total struct size in bytes (0 — empty base).</summary>
    public const int TotalBytes = 0;
}

/// <summary>
/// Empty selector tag for anonymous (general-purpose) slab allocations:
/// <code>
/// struct ZAllocAnonSelector {};  // sizeof = 0
/// </code>
/// </summary>
/// <remarks>PDB source: <c>game_types.h</c> line 17664 (ordinal 1550).</remarks>
public readonly ref struct ZAllocAnonSelectorLayout
{
    /// <summary>Total struct size in bytes (0 — empty tag).</summary>
    public const int TotalBytes = 0;
}

/// <summary>
/// Empty selector tag for narrow (char) string slab allocations:
/// <code>
/// struct ZAllocStrSelector&lt;char&gt; {};          // sizeof = 0
/// struct ZAllocStrSelector&lt;unsigned short&gt; {}; // sizeof = 0 (wide variant)
/// </code>
/// </summary>
/// <remarks>
/// PDB source: <c>game_types.h</c> line 18357 (ordinal 1604).
/// Wide-char variant at line 18375 (ordinal 1607) is structurally identical.
/// </remarks>
public readonly ref struct ZAllocStrSelectorLayout
{
    /// <summary>Total struct size in bytes (0 — empty tag).</summary>
    public const int TotalBytes = 0;
}

/// <summary>
/// Empty helper passed as an allocator hint parameter:
/// <code>
/// struct ZAllocHelper {};  // sizeof = 0
/// </code>
/// </summary>
/// <remarks>PDB source: <c>game_types.h</c> line 19664 (ordinal 1762).</remarks>
public readonly ref struct ZAllocHelperLayout
{
    /// <summary>Total struct size in bytes (0 — empty helper).</summary>
    public const int TotalBytes = 0;
}

/// <summary>
/// Memory layout of <c>ZAllocEx&lt;TSelector&gt;</c> — the per-thread slab arena
/// used for ZXString and ZMap node allocations:
/// <code>
/// struct ZAllocEx&lt;TSelector&gt; : ZAllocBase, TSelector  // both empty — EBO applies to one
/// {
///     _BYTE         gap0;             // +0x00  empty-base padding (1 byte + 3-byte align pad)
///     ZFatalSection m_lock;           // +0x04  reentrant spin lock (8 bytes: TIB ptr + nRef)
///     void         *m_apBuff[4];      // +0x0C  slab buffer pointers  (4 × 4 = 16 bytes)
///     void         *m_apBlockHead[4]; // +0x1C  block-chain heads     (4 × 4 = 16 bytes)
/// };  // sizeof = 0x2C
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// PDB lines: <c>ZAllocEx&lt;ZAllocAnonSelector&gt;</c> 17678 (ordinal 1553),
/// <c>ZAllocEx&lt;ZAllocStrSelector&lt;char&gt;&gt;</c> 18361 (ordinal 1605),
/// <c>ZAllocEx&lt;ZAllocStrSelector&lt;unsigned short&gt;&gt;</c> 18378 (ordinal 1608).
/// All three instantiations share an identical field layout.
/// </para>
/// <para>
/// This type is a <b>runtime-only</b> structure. Its fields are meaningful only
/// in a live process; offline analysis does not need to decode its state.
/// </para>
/// </remarks>
public readonly ref struct ZAllocExLayout
{
    /// <summary>Byte offset of the empty-base-class padding (<c>gap0</c> — 1 visible byte).</summary>
    public const int GapOffset = 0;

    /// <summary>
    /// Byte offset of <c>m_lock</c> (<see cref="ZFatalSectionLayout"/>).
    /// Placed at +0x04 due to 4-byte pointer alignment after <c>gap0</c>.
    /// </summary>
    public const int LockOffset = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>m_apBuff[0]</c> — first slab-buffer pointer.</summary>
    public const int BuffOffset = LockOffset + ZFatalSectionLayout.TotalBytes;

    /// <summary>Number of <c>m_apBuff</c> slots (4).</summary>
    public const int BuffCount = 4;

    /// <summary>Byte offset of <c>m_apBlockHead[0]</c> — first block-chain head pointer.</summary>
    public const int BlockHeadOffset = BuffOffset + BuffCount * TypeSizes.Pointer;

    /// <summary>Number of <c>m_apBlockHead</c> slots (4).</summary>
    public const int BlockHeadCount = 4;

    /// <summary>Total struct size in bytes (0x2C = 44).</summary>
    public const int TotalBytes = BlockHeadOffset + BlockHeadCount * TypeSizes.Pointer;
}
