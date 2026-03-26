using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>ZSocketBase</c>:
/// <code>
/// struct ZSocketBase {
///     unsigned int _m_hSocket;   // +0x00  native Win32 socket handle
/// };  // sizeof = 0x04
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 24403.
/// </remarks>
public readonly ref struct ZSocketBaseLayout
{
    /// <summary>Byte offset of <c>_m_hSocket</c> — Win32 socket handle.</summary>
    public const int SocketHandleOffset = 0;

    /// <summary>Total struct size in bytes (4).</summary>
    public const int TotalBytes = TypeSizes.Pointer;
}

/// <summary>
/// Mirrors <c>ZSocketBase</c>.
/// </summary>
public readonly struct ZSocketBase : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => ZSocketBaseLayout.TotalBytes;

    /// <summary>Win32 socket handle (<c>_m_hSocket</c>).</summary>
    public uint SocketHandle { get; }

    /// <summary>Creates a <see cref="ZSocketBase"/> with the specified socket handle.</summary>
    /// <param name="socketHandle">Win32 socket handle.</param>
    public ZSocketBase(uint socketHandle) => SocketHandle = socketHandle;

    /// <summary>Reads a <c>ZSocketBase</c> from binary data at <paramref name="fileOffset"/>.</summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="fileOffset">File offset of the struct start.</param>
    public static ZSocketBase ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZSocketBaseLayout.SocketHandleOffset)..]));
}
