namespace Maple.Native.Test;

public class ZAllocTests
{
    [Test]
    public async Task AllocBaseLayout_IsZeroBytes()
    {
        int actual = ZAllocBaseLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task AnonSelectorLayout_IsZeroBytes()
    {
        int actual = ZAllocAnonSelectorLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task StrSelectorLayout_IsZeroBytes()
    {
        int actual = ZAllocStrSelectorLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task HelperLayout_IsZeroBytes()
    {
        int actual = ZAllocHelperLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task ExLayout_TotalBytes_Is44()
    {
        int actual = ZAllocExLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(44);
    }

    [Test]
    public async Task ExLayout_LockOffset_Is4()
    {
        int actual = ZAllocExLayout.LockOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task ExLayout_BuffOffset_Is12()
    {
        int actual = ZAllocExLayout.BuffOffset;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task ExLayout_BuffCount_Is4()
    {
        int actual = ZAllocExLayout.BuffCount;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task ExLayout_BlockHeadOffset_Is28()
    {
        int actual = ZAllocExLayout.BlockHeadOffset;
        await Assert.That(actual).IsEqualTo(28);
    }

    [Test]
    public async Task ExLayout_BlockHeadCount_Is4()
    {
        int actual = ZAllocExLayout.BlockHeadCount;
        await Assert.That(actual).IsEqualTo(4);
    }
}
