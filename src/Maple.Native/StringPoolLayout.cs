namespace Maple.Native;

/// <summary>
/// Memory layout of the C++ <c>StringPool</c> object:
/// <code>
/// struct StringPool : ClassLevelLockable&lt;StringPool&gt;  // empty base — zero bytes
/// {
///     ZArray&lt;ZXString&lt;char&gt; *&gt;           m_apZMString;   // +0x00  (4 bytes — pointer)
///     ZArray&lt;ZXString&lt;unsigned short&gt; *&gt; m_apZWString;   // +0x04  (4 bytes — pointer)
///     ZFatalSection                        m_lock;         // +0x08  (8 bytes — TIB+ref)
/// };  // sizeof = 0x10
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// PDB source: <c>game_types.h</c> line 67186 (ordinal 8089).
/// </para>
/// <para>
/// <c>ClassLevelLockable&lt;StringPool&gt;</c> is an empty CRTP base with a static
/// <c>ms_nLocker</c> (volatile LONG) — contributes zero bytes to the instance layout.
/// </para>
/// <para>
/// The static members (<c>ms_aString</c>, <c>ms_aKey</c>, <c>ms_nKeySize</c>,
/// <c>ms_nSize</c>) reside at fixed addresses in the <c>.data</c> section and are
/// described separately by <c>StringPoolAddresses</c>.
/// </para>
/// </remarks>
public readonly ref struct StringPoolLayout
{
    /// <summary>
    /// Fixed slot count used by the GMS v95 client constructor for both StringPool caches.
    /// </summary>
    public const int GmsV95SlotCount = 0x1AE3;

    /// <summary>Byte offset of <c>m_apZMString</c> (narrow string cache pointer).</summary>
    public const int NarrowCacheOffset = 0;

    /// <summary>Byte offset of <c>m_apZWString</c> (wide string cache pointer).</summary>
    public const int WideCacheOffset = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>m_lock</c> (<see cref="ZFatalSectionLayout"/>).</summary>
    public const int LockOffset = TypeSizes.Pointer * 2;

    /// <summary>Total struct size: 0x10 (two pointers + <see cref="ZFatalSectionLayout.TotalBytes"/>).</summary>
    public const int TotalBytes = TypeSizes.Pointer * 2 + ZFatalSectionLayout.TotalBytes;
}

/// <summary>
/// Memory layout of <c>StringPool::Key</c>:
/// <code>
/// struct StringPool::Key {
///     ZArray&lt;unsigned char&gt; m_aKey;   // +0x00  (4 bytes — pointer to rotated key bytes)
/// };
/// </code>
/// </summary>
/// <remarks>
/// PDB source: <c>game_types.h</c> line 67197 (ordinal 8091).
/// </remarks>
public readonly ref struct StringPoolKeyLayout
{
    /// <summary>Byte offset of <c>m_aKey</c> (the <c>ZArray&lt;unsigned char&gt;</c> pointer).</summary>
    public const int KeyArrayOffset = 0;

    /// <summary>Total struct size (one pointer = 4 bytes).</summary>
    public const int TotalBytes = TypeSizes.Pointer;
}
