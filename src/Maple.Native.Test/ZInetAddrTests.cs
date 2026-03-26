namespace Maple.Native.Test;

public class ZInetAddrTests
{
    [Test]
    public async Task Layout_TotalBytes_Is16()
    {
        int actual = ZInetAddrLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(16);
    }

    [Test]
    public async Task Layout_FamilyOffset_Is0()
    {
        int actual = ZInetAddrLayout.FamilyOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task Layout_PortOffset_Is2()
    {
        int actual = ZInetAddrLayout.PortOffset;
        await Assert.That(actual).IsEqualTo(2);
    }

    [Test]
    public async Task Layout_AddrOffset_Is4()
    {
        int actual = ZInetAddrLayout.AddrOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task NativeSize_Is16()
    {
        await Assert.That(ZInetAddr.NativeSize).IsEqualTo(16);
    }

    [Test]
    public async Task Port_ConvertsFromNetworkByteOrder()
    {
        var bytes = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(0), 2); // AF_INET
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), 8484); // port in network order

        var addr = ZInetAddr.ReadFrom(bytes, 0);

        await Assert.That(addr.Port).IsEqualTo((ushort)8484);
        await Assert.That(addr.Family).IsEqualTo((short)2);
    }

    [Test]
    public async Task ReadFrom_DecodesAllFields()
    {
        var bytes = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(0), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), 8484);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4), 0x7F000001u); // 127.0.0.1

        var addr = ZInetAddr.ReadFrom(bytes, 0);

        await Assert.That(addr.Family).IsEqualTo((short)2);
        await Assert.That(addr.Port).IsEqualTo((ushort)8484);
        await Assert.That(addr.Address).IsNotEqualTo(0u);
    }
}
