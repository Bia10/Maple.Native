namespace Maple.Native.Test;

public class ZThreadTests
{
    [Test]
    public async Task Layout_TotalBytes_Is12()
    {
        int actual = ZThreadLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task Layout_ThreadIdOffset_Is4()
    {
        int actual = ZThreadLayout.ThreadIdOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Layout_ThreadHandleOffset_Is8()
    {
        int actual = ZThreadLayout.ThreadHandleOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task NativeSize_Is12()
    {
        await Assert.That(ZThread.NativeSize).IsEqualTo(12);
    }

    [Test]
    public async Task ReadFrom_DecodesAllFields()
    {
        var bytes = new byte[12];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x00401000u); // vtbl
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0x1234u); // threadId
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0xABCDu); // handle

        var thread = ZThread.ReadFrom(bytes, 0);

        await Assert.That(thread.VTablePointer).IsEqualTo(0x00401000u);
        await Assert.That(thread.ThreadId).IsEqualTo(0x1234u);
        await Assert.That(thread.ThreadHandle).IsEqualTo(0xABCDu);
    }
}
