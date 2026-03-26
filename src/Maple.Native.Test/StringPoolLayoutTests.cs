namespace Maple.Native.Test;

public class StringPoolLayoutTests
{
    [Test]
    public async Task GmsV95SlotCount_Is6883()
    {
        int actual = StringPoolLayout.GmsV95SlotCount;
        await Assert.That(actual).IsEqualTo(6883);
    }

    [Test]
    public async Task NarrowCacheOffset_Is0()
    {
        int actual = StringPoolLayout.NarrowCacheOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task WideCacheOffset_Is4()
    {
        int actual = StringPoolLayout.WideCacheOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task LockOffset_Is8()
    {
        int actual = StringPoolLayout.LockOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task TotalBytes_Is16()
    {
        int actual = StringPoolLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(16);
    }

    [Test]
    public async Task KeyLayout_TotalBytes_Is4()
    {
        int actual = StringPoolKeyLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(4);
    }
}
