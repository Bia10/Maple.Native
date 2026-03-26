using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>ZMap&lt;K,V,H&gt;</c> — the Maple open-addressing hash map:
/// <code>
/// struct ZMap&lt;K,V,H&gt;
/// {
///     ZMap_vtbl    *__vftable;              // +0x00  virtual destructor table  (4 bytes)
///     _PAIR       **_m_apTable;             // +0x04  bucket array pointer       (4 bytes)
///     unsigned int  _m_uTableSize;          // +0x08  bucket array length        (4 bytes)
///     unsigned int  _m_uCount;              // +0x0C  total stored entries        (4 bytes)
///     unsigned int  _m_uAutoGrowEvery128;   // +0x10  auto-grow modulo counter   (4 bytes)
///     unsigned int  _m_uAutoGrowLimit;      // +0x14  load-factor grow threshold (4 bytes)
/// };  // sizeof = 0x18
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// PDB lines: <c>ZMap&lt;long,long,long&gt;</c> 21236 (ordinal 1900),
/// <c>ZMap&lt;ZXString&lt;char&gt;,ZPair&lt;long,long&gt;,ZXString&lt;char&gt;&gt;</c> 23066 (ordinal 2133).
/// All instantiations share the same header layout regardless of key/value types.
/// </para>
/// <para>
/// <c>_m_apTable</c> points to a heap-allocated <c>_PAIR*[_m_uTableSize]</c> array.
/// Each non-null element is the head of a singly-linked bucket chain;
/// see <see cref="ZMapPairLayout"/> for node layout.
/// </para>
/// </remarks>
public readonly ref struct ZMapLayout
{
    /// <summary>Byte offset of the virtual function table pointer.</summary>
    public const int VTableOffset = 0;

    /// <summary>
    /// Byte offset of <c>_m_apTable</c> — pointer to the heap-allocated bucket array.
    /// Each slot is a <c>_PAIR*</c> (head of a bucket chain, or null for an empty bucket).
    /// </summary>
    public const int TableOffset = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>_m_uTableSize</c> — number of buckets.</summary>
    public const int TableSizeOffset = TypeSizes.Pointer * 2;

    /// <summary>Byte offset of <c>_m_uCount</c> — total number of stored key-value pairs.</summary>
    public const int CountOffset = TypeSizes.Pointer * 2 + TypeSizes.Int32;

    /// <summary>Byte offset of <c>_m_uAutoGrowEvery128</c>.</summary>
    public const int AutoGrowEvery128Offset = TypeSizes.Pointer * 2 + TypeSizes.Int32 * 2;

    /// <summary>Byte offset of <c>_m_uAutoGrowLimit</c>.</summary>
    public const int AutoGrowLimitOffset = TypeSizes.Pointer * 2 + TypeSizes.Int32 * 3;

    /// <summary>Total struct size in bytes (0x18 = 24).</summary>
    public const int TotalBytes = TypeSizes.Pointer * 2 + TypeSizes.Int32 * 4;
}

/// <summary>
/// Memory layout of <c>ZMap&lt;K,V,H&gt;::_PAIR</c> — a single bucket-chain node:
/// <code>
/// struct _PAIR : ZRecyclable&lt;_PAIR, 16, _PAIR&gt;  // base contributes vtbl* at +0x00
/// {
///     _PAIR *pNext;   // +0x04  next node in same bucket (null = end of chain)
///     K      key;     // +0x08  key value
///     V      value;   // +0x08 + sizeof(K)  associated value (natural alignment)
/// };
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Common GMS v95 instantiations:
/// <list type="table">
///   <listheader>
///     <term>C++ type</term>
///     <description>key / value bytes → sizeof(_PAIR)</description>
///   </listheader>
///   <item>
///     <term><c>ZMap&lt;long,long,long&gt;</c></term>
///     <description>4 / 4 → 0x10 — PDB line 21268 (ordinal 1903)</description>
///   </item>
///   <item>
///     <term><c>ZMap&lt;ZXString&lt;char&gt;,ZPair&lt;long,long&gt;,ZXString&lt;char&gt;&gt;</c></term>
///     <description>4 / 8 → 0x14 — PDB line 23096 (ordinal 2138)</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Use the factory properties <see cref="IntInt"/> and <see cref="XStringZPair"/>
/// to obtain pre-configured layouts for the two most common instantiations,
/// or construct with explicit <c>keyBytes</c> / <c>valueBytes</c> for others.
/// </para>
/// </remarks>
/// <param name="keyBytes">Size of the key type in bytes.</param>
/// <param name="valueBytes">Size of the value type in bytes.</param>
public readonly ref struct ZMapPairLayout(int keyBytes, int valueBytes)
{
    /// <summary>Byte offset of the vtable pointer (from <c>ZRecyclable</c> base).</summary>
    public const int VTableOffset = 0;

    /// <summary>
    /// Byte offset of <c>pNext</c> — the singly-linked bucket chain pointer
    /// (null = last node in this bucket).
    /// </summary>
    public const int NextOffset = TypeSizes.Pointer;

    /// <summary>Byte offset where the key field begins.</summary>
    public const int KeyOffset = TypeSizes.Pointer * 2;

    /// <summary>Size of the key field in bytes.</summary>
    public int KeyBytes { get; } = keyBytes;

    /// <summary>Byte offset where the value field begins (immediately after the key).</summary>
    public int ValueOffset => KeyOffset + KeyBytes;

    /// <summary>Size of the value field in bytes.</summary>
    public int ValueBytes { get; } = valueBytes;

    /// <summary>Total struct size in bytes.</summary>
    public int TotalBytes => KeyOffset + KeyBytes + ValueBytes;

    // ── Pre-built factory properties ──────────────────────────────────────────

    /// <summary>
    /// Layout for <c>ZMap&lt;long,long,long&gt;::_PAIR</c>: 4-byte int key + 4-byte int value.
    /// <c>sizeof = 0x10</c>.
    /// </summary>
    public static ZMapPairLayout IntInt => new(TypeSizes.Int32, TypeSizes.Int32);

    /// <summary>
    /// Layout for <c>ZMap&lt;ZXString&lt;char&gt;,ZPair&lt;long,long&gt;,ZXString&lt;char&gt;&gt;::_PAIR</c>:
    /// 4-byte pointer key (<c>ZXString._m_pStr</c>) + 8-byte value (<c>ZPair&lt;long,long&gt;</c>).
    /// <c>sizeof = 0x14</c>.
    /// </summary>
    public static ZMapPairLayout XStringZPair => new(TypeSizes.Pointer, TypeSizes.Int32 * 2);
}

/// <summary>
/// C# representation of a decoded <c>ZMap</c> header.
/// </summary>
/// <param name="tableVa">Runtime VA of the bucket array.</param>
/// <param name="tableSize">Number of buckets.</param>
/// <param name="count">Total number of stored key-value pairs.</param>
/// <param name="autoGrowEvery128">Auto-grow modulo counter.</param>
/// <param name="autoGrowLimit">Entry-count threshold that triggers a grow.</param>
public readonly struct ZMapHeader(uint tableVa, uint tableSize, uint count, uint autoGrowEvery128, uint autoGrowLimit)
{
    /// <summary>Runtime VA of the <c>_PAIR*[]</c> bucket array.</summary>
    public uint TableVa { get; } = tableVa;

    /// <summary>Number of buckets in the array.</summary>
    public uint TableSize { get; } = tableSize;

    /// <summary>Total number of stored key-value pairs.</summary>
    public uint Count { get; } = count;

    /// <summary>Auto-grow modulo counter.</summary>
    public uint AutoGrowEvery128 { get; } = autoGrowEvery128;

    /// <summary>Entry-count threshold that triggers a bucket-array resize.</summary>
    public uint AutoGrowLimit { get; } = autoGrowLimit;

    /// <summary>
    /// Reads a <c>ZMap</c> header from <paramref name="image"/> at <paramref name="fileOffset"/>.
    /// </summary>
    public static ZMapHeader ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZMapLayout.TableOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZMapLayout.TableSizeOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZMapLayout.CountOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZMapLayout.AutoGrowEvery128Offset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZMapLayout.AutoGrowLimitOffset)..])
        );
}

/// <summary>
/// Static helpers for reading <c>ZMap</c> bucket arrays and chain nodes.
/// </summary>
public static class ZMap
{
    /// <summary>
    /// Reads the <c>_PAIR*</c> bucket head at slot <paramref name="bucketIndex"/>
    /// from the bucket array whose first element is at <paramref name="tableFileOffset"/>.
    /// Returns the VA of the first <c>_PAIR</c> node in that bucket, or 0 if empty.
    /// </summary>
    public static uint ReadBucketHead(ReadOnlySpan<byte> image, int tableFileOffset, int bucketIndex) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(tableFileOffset + bucketIndex * TypeSizes.Pointer)..]);

    /// <summary>
    /// Reads the <c>pNext</c> chain link from a <c>_PAIR</c> node stored at
    /// <paramref name="nodeFileOffset"/>.
    /// Returns the VA of the next node in the same bucket, or 0 if this is the last node.
    /// </summary>
    public static uint ReadPairNext(ReadOnlySpan<byte> image, int nodeFileOffset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(nodeFileOffset + ZMapPairLayout.NextOffset)..]);
}
