namespace Maple.Native.Test;

public class ZRefTests
{
    [Test]
    public async Task Layout_TotalBytes_Is8()
    {
        int actual = ZRefLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task Layout_GapBytes_Is4()
    {
        int actual = ZRefLayout.GapBytes;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Layout_PointerOffset_Is4()
    {
        int actual = ZRefLayout.PointerOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task ReadPointer_DecodesInnerPointer()
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0x00403000u);

        var ptr = ZRef.ReadPointer(bytes, 0);

        await Assert.That(ptr).IsEqualTo(0x00403000u);
    }
}
