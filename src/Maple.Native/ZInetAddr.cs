using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>ZInetAddr</c>, which inherits from <c>sockaddr_in</c>:
/// <code>
/// struct ZInetAddr : sockaddr_in {
///     // sockaddr_in base (all fields):
///     short          sin_family;    // +0x00  address family (AF_INET = 2)
///     unsigned short sin_port;      // +0x02  port in network byte order (big-endian)
///     in_addr        sin_addr;      // +0x04  IPv4 address as 4-byte big-endian value
///     char           sin_zero[8];   // +0x08  padding (must be zeroed)
/// };  // sizeof = 0x10
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 24464.
/// <c>ZInetAddr</c> adds no fields of its own — it is purely a typed wrapper over
/// <c>sockaddr_in</c>. <c>sin_port</c> and <c>sin_addr</c> are stored in network
/// byte order (big-endian) as per BSD socket conventions.
/// </remarks>
public readonly ref struct ZInetAddrLayout
{
    private const int Int16Size = 2;

    /// <summary>Byte offset of <c>sin_family</c> — address family.</summary>
    public const int FamilyOffset = 0;

    /// <summary>Byte offset of <c>sin_port</c> — port in network byte order.</summary>
    public const int PortOffset = FamilyOffset + Int16Size;

    /// <summary>Byte offset of <c>sin_addr</c> — 32-bit IPv4 address in network byte order.</summary>
    public const int AddrOffset = PortOffset + Int16Size;

    /// <summary>Byte offset of <c>sin_zero[8]</c> — reserved padding.</summary>
    public const int ZeroOffset = AddrOffset + TypeSizes.Int32;

    /// <summary>Total struct size in bytes (0x10 = 16).</summary>
    public const int TotalBytes = ZeroOffset + 8;
}

/// <summary>
/// Mirrors <c>ZInetAddr</c>.
/// </summary>
public readonly struct ZInetAddr : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => ZInetAddrLayout.TotalBytes;

    /// <summary>Address family (<c>sin_family</c>); <c>AF_INET = 2</c>.</summary>
    public short Family { get; }

    /// <summary>Port in network byte order (<c>sin_port</c>).</summary>
    public ushort PortNetworkOrder { get; }

    /// <summary>Port in host byte order — bytes swapped from <see cref="PortNetworkOrder"/>.</summary>
    public ushort Port => (ushort)((PortNetworkOrder >> 8) | ((PortNetworkOrder & 0xFF) << 8));

    /// <summary>IPv4 address in network byte order (<c>sin_addr.s_addr</c>).</summary>
    public uint Address { get; }

    /// <summary>Creates a <see cref="ZInetAddr"/> with the specified fields.</summary>
    /// <param name="family">Address family.</param>
    /// <param name="portNetworkOrder">Port in network byte order.</param>
    /// <param name="address">IPv4 address in network byte order.</param>
    public ZInetAddr(short family, ushort portNetworkOrder, uint address)
    {
        Family = family;
        PortNetworkOrder = portNetworkOrder;
        Address = address;
    }

    /// <summary>Reads a <c>ZInetAddr</c> from binary data at <paramref name="fileOffset"/>.</summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="fileOffset">File offset of the struct start.</param>
    public static ZInetAddr ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadInt16LittleEndian(image[(fileOffset + ZInetAddrLayout.FamilyOffset)..]),
            BinaryPrimitives.ReadUInt16LittleEndian(image[(fileOffset + ZInetAddrLayout.PortOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZInetAddrLayout.AddrOffset)..])
        );
}
