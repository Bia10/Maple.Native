using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>COutPacket</c>:
/// <code>
/// struct COutPacket {
///     int                   m_bLoopback;            // +0x00  non-zero if loopback packet
///     ZArray&lt;unsigned char&gt; m_aSendBuff;            // +0x04  send-buffer array pointer
///     unsigned int          m_uOffset;               // +0x08  current write position
///     int                   m_bIsEncryptedByShanda;  // +0x0C  Shanda XOR flag
/// };  // sizeof = 0x10
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 20195.
/// <c>m_aSendBuff</c> is a <c>ZArray&lt;unsigned char&gt;</c> — a single pointer whose
/// element count lives at <c>pointer - 4</c>. Use <see cref="ZArray"/> to read it.
/// </remarks>
public readonly ref struct COutPacketLayout
{
    /// <summary>Byte offset of <c>m_bLoopback</c>.</summary>
    public const int LoopbackOffset = 0;

    /// <summary>Byte offset of <c>m_aSendBuff</c> — <c>ZArray</c> payload pointer.</summary>
    public const int SendBuffOffset = TypeSizes.Int32;

    /// <summary>Byte offset of <c>m_uOffset</c> — current write position.</summary>
    public const int WriteOffsetOffset = SendBuffOffset + TypeSizes.Pointer;

    /// <summary>Byte offset of <c>m_bIsEncryptedByShanda</c>.</summary>
    public const int ShandaFlagOffset = WriteOffsetOffset + TypeSizes.Int32;

    /// <summary>Total struct size in bytes (0x10 = 16).</summary>
    public const int TotalBytes = ShandaFlagOffset + TypeSizes.Int32;
}

/// <summary>
/// Mirrors <c>COutPacket</c>.
/// </summary>
public readonly struct COutPacket : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => COutPacketLayout.TotalBytes;

    /// <summary>Non-zero if this is a loopback packet (<c>m_bLoopback</c>).</summary>
    public bool IsLoopback { get; }

    /// <summary>Payload pointer of <c>m_aSendBuff</c> — the raw <c>ZArray</c> element pointer.</summary>
    public uint SendBuffPointer { get; }

    /// <summary>Current write offset into the send buffer (<c>m_uOffset</c>).</summary>
    public uint WriteOffset { get; }

    /// <summary>Non-zero if the buffer has been Shanda-encrypted (<c>m_bIsEncryptedByShanda</c>).</summary>
    public bool IsEncryptedByShanda { get; }

    /// <summary>Creates a <see cref="COutPacket"/> with the specified fields.</summary>
    public COutPacket(bool isLoopback, uint sendBuffPointer, uint writeOffset, bool isEncryptedByShanda)
    {
        IsLoopback = isLoopback;
        SendBuffPointer = sendBuffPointer;
        WriteOffset = writeOffset;
        IsEncryptedByShanda = isEncryptedByShanda;
    }

    /// <summary>Reads a <c>COutPacket</c> from binary data at <paramref name="fileOffset"/>.</summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="fileOffset">File offset of the struct start.</param>
    public static COutPacket ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + COutPacketLayout.LoopbackOffset)..]) != 0,
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + COutPacketLayout.SendBuffOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + COutPacketLayout.WriteOffsetOffset)..]),
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + COutPacketLayout.ShandaFlagOffset)..]) != 0
        );
}

/// <summary>
/// Memory layout of <c>CInPacket</c>:
/// <code>
/// struct CInPacket {
///     int                   m_bLoopback;   // +0x00  non-zero if loopback packet
///     int                   m_nState;      // +0x04  decode state machine state
///     ZArray&lt;unsigned char&gt; m_aRecvBuff;  // +0x08  receive-buffer array pointer
///     unsigned __int16      m_uLength;     // +0x0C  total packet length
///     unsigned __int16      m_uRawSeq;     // +0x0E  raw sequence number
///     unsigned __int16      m_uDataLen;    // +0x10  data segment length
///                                          // +0x12  2-byte alignment padding (uint follows)
///     unsigned int          m_uOffset;     // +0x14  current read position
/// };  // sizeof = 0x18
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 20203.
/// <c>m_uOffset</c> lands at +0x14 because three consecutive <c>uint16</c> fields (6 bytes)
/// require 2 bytes of implicit alignment padding before the next <c>uint32</c>.
/// </remarks>
public readonly ref struct CInPacketLayout
{
    private const int Int16Size = 2;

    /// <summary>Byte offset of <c>m_bLoopback</c>.</summary>
    public const int LoopbackOffset = 0;

    /// <summary>Byte offset of <c>m_nState</c>.</summary>
    public const int StateOffset = TypeSizes.Int32;

    /// <summary>Byte offset of <c>m_aRecvBuff</c> — <c>ZArray</c> payload pointer.</summary>
    public const int RecvBuffOffset = StateOffset + TypeSizes.Int32;

    /// <summary>Byte offset of <c>m_uLength</c> — total packet length.</summary>
    public const int LengthOffset = RecvBuffOffset + TypeSizes.Pointer;

    /// <summary>Byte offset of <c>m_uRawSeq</c> — raw sequence number.</summary>
    public const int RawSeqOffset = LengthOffset + Int16Size;

    /// <summary>Byte offset of <c>m_uDataLen</c> — data segment length.</summary>
    public const int DataLenOffset = RawSeqOffset + Int16Size;

    /// <summary>
    /// Byte offset of <c>m_uOffset</c> — current read position.
    /// Placed at +0x14: after the three <c>uint16</c> fields (6 bytes) plus 2-byte
    /// alignment padding to reach the next 4-byte boundary.
    /// </summary>
    public const int ReadOffsetOffset = DataLenOffset + Int16Size + Int16Size; // +2 padding

    /// <summary>Total struct size in bytes (0x18 = 24).</summary>
    public const int TotalBytes = ReadOffsetOffset + TypeSizes.Int32;
}

/// <summary>
/// Mirrors <c>CInPacket</c>.
/// </summary>
public readonly struct CInPacket : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => CInPacketLayout.TotalBytes;

    /// <summary>Non-zero if this is a loopback packet (<c>m_bLoopback</c>).</summary>
    public bool IsLoopback { get; }

    /// <summary>Decode state machine state (<c>m_nState</c>).</summary>
    public int State { get; }

    /// <summary>Payload pointer of <c>m_aRecvBuff</c> — the raw <c>ZArray</c> element pointer.</summary>
    public uint RecvBuffPointer { get; }

    /// <summary>Total packet length (<c>m_uLength</c>).</summary>
    public ushort Length { get; }

    /// <summary>Raw sequence number (<c>m_uRawSeq</c>).</summary>
    public ushort RawSeq { get; }

    /// <summary>Data segment length (<c>m_uDataLen</c>).</summary>
    public ushort DataLen { get; }

    /// <summary>Current read offset into the receive buffer (<c>m_uOffset</c>).</summary>
    public uint ReadOffset { get; }

    /// <summary>Creates a <see cref="CInPacket"/> with the specified fields.</summary>
    public CInPacket(
        bool isLoopback,
        int state,
        uint recvBuffPointer,
        ushort length,
        ushort rawSeq,
        ushort dataLen,
        uint readOffset
    )
    {
        IsLoopback = isLoopback;
        State = state;
        RecvBuffPointer = recvBuffPointer;
        Length = length;
        RawSeq = rawSeq;
        DataLen = dataLen;
        ReadOffset = readOffset;
    }

    /// <summary>Reads a <c>CInPacket</c> from binary data at <paramref name="fileOffset"/>.</summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="fileOffset">File offset of the struct start.</param>
    public static CInPacket ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + CInPacketLayout.LoopbackOffset)..]) != 0,
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + CInPacketLayout.StateOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + CInPacketLayout.RecvBuffOffset)..]),
            BinaryPrimitives.ReadUInt16LittleEndian(image[(fileOffset + CInPacketLayout.LengthOffset)..]),
            BinaryPrimitives.ReadUInt16LittleEndian(image[(fileOffset + CInPacketLayout.RawSeqOffset)..]),
            BinaryPrimitives.ReadUInt16LittleEndian(image[(fileOffset + CInPacketLayout.DataLenOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + CInPacketLayout.ReadOffsetOffset)..])
        );
}
