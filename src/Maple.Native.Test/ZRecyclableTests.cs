namespace Maple.Native.Test;

public class ZRecyclableTests
{
    [Test]
    public async Task RecyclableLayout_TotalBytes_Is4()
    {
        int actual = ZRecyclableLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task RecyclableStaticLayout_TotalBytes_Is4()
    {
        int actual = ZRecyclableStaticLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task DummyLayout_DataOffset_Is16()
    {
        int actual = ZRefCountedDummyLayout.DataOffset;
        await Assert.That(actual).IsEqualTo(16);
    }

    [Test]
    public async Task DummyLayout_HeaderBytes_Is16()
    {
        int actual = ZRefCountedDummyLayout.HeaderBytes;
        await Assert.That(actual).IsEqualTo(16);
    }

    [Test]
    public async Task DummyLayout_VTableOffset_Is0()
    {
        int actual = ZRefCountedDummyLayout.VTableOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task DummyLayout_RecyclableVTableOffset_Is12()
    {
        int actual = ZRefCountedDummyLayout.RecyclableVTableOffset;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task DummyHeader_ReadFrom_DecodesAllPointers()
    {
        var bytes = new byte[ZRefCountedDummyLayout.HeaderBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x00401000u); // vtbl
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0x00402000u); // next
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0u); // prev (tail)
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 0x00403000u); // recyclable vtbl

        var header = ZRefCountedDummyHeader.ReadFrom(bytes, 0);

        await Assert.That(header.VTablePointer).IsEqualTo(0x00401000u);
        await Assert.That(header.NextPointer).IsEqualTo(0x00402000u);
        await Assert.That(header.PrevPointer).IsEqualTo(0u);
        await Assert.That(header.RecyclableVTablePointer).IsEqualTo(0x00403000u);
    }
}
