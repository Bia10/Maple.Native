namespace Maple.Native.Test;

public class ZListTests
{
    [Test]
    public async Task Layout_TotalBytes_Is20()
    {
        int actual = ZListLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(20);
    }

    [Test]
    public async Task Layout_CountOffset_Is8()
    {
        int actual = ZListLayout.CountOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task Layout_HeadOffset_Is12()
    {
        int actual = ZListLayout.HeadOffset;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task Layout_TailOffset_Is16()
    {
        int actual = ZListLayout.TailOffset;
        await Assert.That(actual).IsEqualTo(16);
    }

    [Test]
    public async Task ReadCount_DecodesCorrectly()
    {
        var bytes = new byte[ZListLayout.TotalBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ZListLayout.CountOffset), 5u);

        var count = ZList.ReadCount(bytes, 0);

        await Assert.That(count).IsEqualTo(5u);
    }

    [Test]
    public async Task ReadHead_DecodesPointer()
    {
        var bytes = new byte[ZListLayout.TotalBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(ZListLayout.HeadOffset),
            0x00401234u
        );

        var head = ZList.ReadHead(bytes, 0);

        await Assert.That(head).IsEqualTo(0x00401234u);
    }

    [Test]
    public async Task ReadTail_DecodesPointer()
    {
        var bytes = new byte[ZListLayout.TotalBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(ZListLayout.TailOffset),
            0x00405678u
        );

        var tail = ZList.ReadTail(bytes, 0);

        await Assert.That(tail).IsEqualTo(0x00405678u);
    }
}
