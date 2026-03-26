namespace Maple.Native.Test;

public class ZFatalSectionTests
{
    [Test]
    public async Task Layout_TotalBytes_Is8()
    {
        int actual = ZFatalSectionLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task Layout_TibPointerOffset_Is0()
    {
        int actual = ZFatalSectionLayout.TibPointerOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task Layout_RefCountOffset_Is4()
    {
        int actual = ZFatalSectionLayout.RefCountOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Unlocked_HasZeroTibAndZeroRefCount()
    {
        var unlocked = ZFatalSection.Unlocked;

        await Assert.That(unlocked.TibPointer).IsEqualTo(0u);
        await Assert.That(unlocked.RefCount).IsEqualTo(0);
    }

    [Test]
    public async Task NativeSize_Is8()
    {
        await Assert.That(ZFatalSection.NativeSize).IsEqualTo(8);
    }

    [Test]
    public async Task ReadFrom_DecodesLockedState()
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0xDEADu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 2);

        var section = NativeCast.Reinterpret<ZFatalSection>(bytes, 0);

        await Assert.That(section.TibPointer).IsEqualTo(0xDEADu);
        await Assert.That(section.RefCount).IsEqualTo(2);
    }

    [Test]
    public async Task ZSyncAutoUnlock_Layout_TotalBytes_Is4()
    {
        int actual = ZSyncAutoUnlockLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(4);
    }
}
