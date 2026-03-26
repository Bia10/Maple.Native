namespace Maple.Native.Test;

public class ZRefCountedTests
{
    [Test]
    public async Task Layout_TotalBytes_Is12()
    {
        int actual = ZRefCountedLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task Layout_VTableOffset_Is0()
    {
        int actual = ZRefCountedLayout.VTableOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task Layout_RefCountOffset_Is4()
    {
        int actual = ZRefCountedLayout.RefCountOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Layout_PrevOffset_Is8()
    {
        int actual = ZRefCountedLayout.PrevOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task NativeSize_Is12()
    {
        await Assert.That(ZRefCounted.NativeSize).IsEqualTo(12);
    }

    [Test]
    public async Task ReadFrom_DecodesAllFields()
    {
        var bytes = new byte[12];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x00401000u); // vtbl
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 3); // refCount
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0x00402000u); // prev

        var obj = ZRefCounted.ReadFrom(bytes, 0);

        await Assert.That(obj.VTablePointer).IsEqualTo(0x00401000u);
        await Assert.That(obj.RefCount).IsEqualTo(3);
        await Assert.That(obj.PrevPointer).IsEqualTo(0x00402000u);
    }
}
