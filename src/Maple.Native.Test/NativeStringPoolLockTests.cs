namespace Maple.Native.Test;

public class NativeStringPoolLockTests
{
    [Test]
    public async Task Read_OnFreshPool_ReturnsUnlockedState()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 1);
        var lockState = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);

        await Assert.That(lockState.TibPointer).IsEqualTo(0u);
        await Assert.That(lockState.RefCount).IsEqualTo(0);

        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }

    [Test]
    public async Task Acquire_LockIsHeld_WhileScopeOpen()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 1);

        using (NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100))
        {
            var locked = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
            await Assert.That(locked.TibPointer).IsNotEqualTo(0u);
            await Assert.That(locked.RefCount).IsEqualTo(1);
        }

        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }

    [Test]
    public async Task Acquire_LockIsReleased_AfterDispose()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 1);

        using (NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100)) { }

        var unlocked = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
        await Assert.That(unlocked.TibPointer).IsEqualTo(0u);
        await Assert.That(unlocked.RefCount).IsEqualTo(0);

        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }
}
