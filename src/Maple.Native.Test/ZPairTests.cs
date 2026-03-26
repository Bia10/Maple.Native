namespace Maple.Native.Test;

public class ZPairTests
{
    [Test]
    public async Task Layout_TotalBytes_Is8()
    {
        int actual = ZPairLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task Layout_FirstOffset_Is0()
    {
        int actual = ZPairLayout.FirstOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task Layout_SecondOffset_Is4()
    {
        int actual = ZPairLayout.SecondOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task NativeSize_Is8()
    {
        await Assert.That(ZPair.NativeSize).IsEqualTo(8);
    }

    [Test]
    public async Task ReadFrom_DecodesCorrectly()
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0), 42);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 99);

        var pair = ZPair.ReadFrom(bytes, 0);

        await Assert.That(pair.First).IsEqualTo(42);
        await Assert.That(pair.Second).IsEqualTo(99);
    }

    [Test]
    public async Task Constructor_SetsProperties()
    {
        var pair = new ZPair(7, 13);

        await Assert.That(pair.First).IsEqualTo(7);
        await Assert.That(pair.Second).IsEqualTo(13);
    }
}
