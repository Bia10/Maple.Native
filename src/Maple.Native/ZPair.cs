using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Mirrors <c>ZPair&lt;long,long&gt;</c>:
/// <code>
/// struct ZPair&lt;long,long&gt; {
///     int first;   // +0x00
///     int second;  // +0x04
/// };  // sizeof = 0x08
/// </code>
/// </summary>
/// <remarks>
/// Used throughout the engine for keyed map entries and paired integer values.
/// In the x86 GMS v95 client <c>long</c> is 4 bytes (<c>sizeof(int)</c>).
/// </remarks>
public readonly ref struct ZPairLayout
{
    /// <summary>Byte offset of <c>first</c>.</summary>
    public const int FirstOffset = 0;

    /// <summary>Byte offset of <c>second</c>.</summary>
    public const int SecondOffset = TypeSizes.Int32;

    /// <summary>Total struct size in bytes (2 × int32 = 8).</summary>
    public const int TotalBytes = TypeSizes.Int32 * 2;
}

/// <summary>
/// C# representation of a <c>ZPair&lt;long,long&gt;</c>.
/// </summary>
/// <param name="first">First element.</param>
/// <param name="second">Second element.</param>
public readonly struct ZPair(int first, int second) : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => ZPairLayout.TotalBytes;

    /// <summary>First element.</summary>
    public int First { get; } = first;

    /// <summary>Second element.</summary>
    public int Second { get; } = second;

    /// <summary>
    /// Reads a <c>ZPair</c> from <paramref name="image"/> at <paramref name="fileOffset"/>.
    /// </summary>
    public static ZPair ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + ZPairLayout.FirstOffset)..]),
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + ZPairLayout.SecondOffset)..])
        );
}
