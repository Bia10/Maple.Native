namespace Maple.Native.Test;

public class NativeStringPoolTests
{
    [Test]
    public async Task AllocateV95_ReturnsNonZeroObjectAddress()
    {
        using var allocator = new InProcessAllocator();

        var alloc = NativeStringPool.AllocateV95(allocator);

        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
    }

    [Test]
    public async Task AllocateV95_SlotCount_IsGmsV95()
    {
        using var allocator = new InProcessAllocator();

        var alloc = NativeStringPool.AllocateV95(allocator);

        await Assert.That(alloc.SlotCount).IsEqualTo(StringPoolLayout.GmsV95SlotCount);
    }

    [Test]
    public async Task AllocateV95_HasNonZeroCachePointers()
    {
        using var allocator = new InProcessAllocator();

        var alloc = NativeStringPool.AllocateV95(allocator);

        await Assert.That(alloc.NarrowCacheBase).IsNotEqualTo(0u);
        await Assert.That(alloc.WideCacheBase).IsNotEqualTo(0u);
        await Assert.That(alloc.NarrowCachePayload).IsNotEqualTo(0u);
        await Assert.That(alloc.WideCachePayload).IsNotEqualTo(0u);
    }

    [Test]
    public async Task AllocateEmpty_RespectsCustomSlotCount()
    {
        using var allocator = new InProcessAllocator();

        var alloc = NativeStringPool.AllocateEmpty(allocator, slotCount: 4);

        await Assert.That(alloc.SlotCount).IsEqualTo(4);
        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
    }

    [Test]
    public async Task CreateV95_ReturnsNonZeroAddress()
    {
        using var allocator = new InProcessAllocator();

        var addr = NativeStringPool.CreateV95(allocator);

        await Assert.That(addr).IsNotEqualTo(0u);
    }

    [Test]
    public async Task SetNarrowSlot_WritesStringPointerToCache()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 2);
        var strAddr = ZXString.Create(allocator, "hello");

        NativeStringPool.SetNarrowSlot(allocator, pool, index: 0, strAddr);

        var written = allocator.ReadUInt32(pool.NarrowCachePayload);
        await Assert.That(written).IsEqualTo(strAddr);

        ZXString.Destroy(allocator, strAddr);
        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }

    [Test]
    public async Task SetWideSlot_WritesStringPointerToCache()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 2);
        var strAddr = ZXStringWide.Create(allocator, "world");

        NativeStringPool.SetWideSlot(allocator, pool, index: 0, strAddr);

        var written = allocator.ReadUInt32(pool.WideCachePayload);
        await Assert.That(written).IsEqualTo(strAddr);

        ZXStringWide.Destroy(allocator, strAddr);
        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }

    [Test]
    public async Task Destroy_DoesNotThrow()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 2);

        await Assert
            .That(() => NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: true, destroyWideStrings: true))
            .ThrowsNothing();
    }
}
