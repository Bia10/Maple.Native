namespace Maple.Native.Test;

public class CPacketTests
{
    // ── COutPacket ──────────────────────────────────────────────────────────

    [Test]
    public async Task OutPacketLayout_TotalBytes_Is16()
    {
        int actual = COutPacketLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(16);
    }

    [Test]
    public async Task OutPacketLayout_SendBuffOffset_Is4()
    {
        int actual = COutPacketLayout.SendBuffOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task OutPacketLayout_WriteOffsetOffset_Is8()
    {
        int actual = COutPacketLayout.WriteOffsetOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task OutPacketLayout_ShandaFlagOffset_Is12()
    {
        int actual = COutPacketLayout.ShandaFlagOffset;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task OutPacket_NativeSize_Is16()
    {
        await Assert.That(COutPacket.NativeSize).IsEqualTo(16);
    }

    [Test]
    public async Task OutPacket_ReadFrom_DecodesAllFields()
    {
        var bytes = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), 1); // loopback
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0x00401000u); // sendBuff
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 64u); // writeOffset
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 0); // shanda off

        var packet = COutPacket.ReadFrom(bytes, 0);

        await Assert.That(packet.IsLoopback).IsTrue();
        await Assert.That(packet.SendBuffPointer).IsEqualTo(0x00401000u);
        await Assert.That(packet.WriteOffset).IsEqualTo(64u);
        await Assert.That(packet.IsEncryptedByShanda).IsFalse();
    }

    [Test]
    public async Task OutPacket_Constructor_SetsAllProperties()
    {
        var packet = new COutPacket(
            isLoopback: false,
            sendBuffPointer: 0x00402000u,
            writeOffset: 128u,
            isEncryptedByShanda: true
        );

        await Assert.That(packet.IsLoopback).IsFalse();
        await Assert.That(packet.SendBuffPointer).IsEqualTo(0x00402000u);
        await Assert.That(packet.WriteOffset).IsEqualTo(128u);
        await Assert.That(packet.IsEncryptedByShanda).IsTrue();
    }

    // ── CInPacket ───────────────────────────────────────────────────────────

    [Test]
    public async Task InPacketLayout_TotalBytes_Is24()
    {
        int actual = CInPacketLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(24);
    }

    [Test]
    public async Task InPacketLayout_RecvBuffOffset_Is8()
    {
        int actual = CInPacketLayout.RecvBuffOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task InPacket_NativeSize_Is24()
    {
        await Assert.That(CInPacket.NativeSize).IsEqualTo(24);
    }

    [Test]
    public async Task InPacket_ReadFrom_DecodesAllFields()
    {
        var bytes = new byte[24];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), 0); // not loopback
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 2); // state
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0x00403000u); // recvBuff
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12), 256); // length
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14), 0x1234); // rawSeq
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16), 252); // dataLen
        // bytes 18-19: padding
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), 4u); // readOffset

        var packet = CInPacket.ReadFrom(bytes, 0);

        await Assert.That(packet.IsLoopback).IsFalse();
        await Assert.That(packet.State).IsEqualTo(2);
        await Assert.That(packet.RecvBuffPointer).IsEqualTo(0x00403000u);
        await Assert.That(packet.Length).IsEqualTo((ushort)256);
        await Assert.That(packet.RawSeq).IsEqualTo((ushort)0x1234);
        await Assert.That(packet.DataLen).IsEqualTo((ushort)252);
        await Assert.That(packet.ReadOffset).IsEqualTo(4u);
    }
}
