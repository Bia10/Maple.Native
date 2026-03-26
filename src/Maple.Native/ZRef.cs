using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Mirrors <c>ZRef&lt;T&gt;</c>:
/// <code>
/// struct ZRef&lt;T&gt; : ZRefCountedAccessor&lt;T&gt;, ZRefCountedAccessor&lt;ZRefCountedDummy&lt;T&gt;&gt;
/// {
///     _BYTE gap0;  // +0x00  empty-base-class padding (1 byte + 3 alignment bytes = 4 total)
///     T *p;        // +0x04  raw pointer to the ref-counted object
/// };  // sizeof = 0x08
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Reference-counting smart pointer. The two empty CRTP bases each require at least
/// one byte under the MSVC ABI when empty-base optimisation cannot be applied (diamond
/// inheritance from <c>ZRefCountedAccessorBase</c>). This manifests as a single
/// padding byte at +0x00 (named <c>gap0</c> in the PDB), followed by 3 implicit
/// alignment bytes, placing <c>p</c> at +0x04.
/// </para>
/// <para>
/// For offline analysis only <c>p</c> (at +0x04) is meaningful.
/// </para>
/// </remarks>
public readonly ref struct ZRefLayout
{
    /// <summary>
    /// Size of the empty-base padding region at the struct head
    /// (1 visible byte + 3 alignment bytes = 4 total).
    /// </summary>
    public const int GapBytes = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>p</c> — the raw object pointer.</summary>
    public const int PointerOffset = GapBytes;

    /// <summary>Total struct size in bytes (gap + pointer = 8).</summary>
    public const int TotalBytes = GapBytes + TypeSizes.Pointer;
}

/// <summary>
/// Typed reader for <c>ZRef&lt;T&gt;</c> pointer fields from a binary image.
/// </summary>
public static class ZRef
{
    /// <summary>
    /// Reads the inner <c>p</c> pointer from a <c>ZRef&lt;T&gt;</c> stored at
    /// <paramref name="fileOffset"/> in <paramref name="image"/>.
    /// </summary>
    public static uint ReadPointer(ReadOnlySpan<byte> image, int fileOffset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZRefLayout.PointerOffset)..]);
}
