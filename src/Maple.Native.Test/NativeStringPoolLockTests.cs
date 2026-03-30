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

    [Test]
    public async Task Acquire_ReentrantSameThread_IncrementsRefCount()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 1);

        using var scope1 = NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100);

        // Same thread acquires again → reentrant path, RefCount should be 2
        using var scope2 = NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100);

        var locked = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
        await Assert.That(locked.RefCount).IsEqualTo(2);

        // Do not call Destroy while scopes are still held; allocator cleanup handles it.
    }

    [Test]
    public async Task Acquire_ReentrantRelease_DecrementsRefCount()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 1);

        var scope1 = NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100);
        var scope2 = NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100);

        // Release inner scope → RefCount drops to 1 (lock still held by scope1)
        scope2.Dispose();

        var stillLocked = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
        await Assert.That(stillLocked.RefCount).IsEqualTo(1);
        await Assert.That(stillLocked.TibPointer).IsNotEqualTo(0u);

        // Release outer scope → lock fully released
        scope1.Dispose();

        var released = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
        await Assert.That(released.TibPointer).IsEqualTo(0u);

        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }

    [Test]
    public async Task Dispose_CalledTwice_IsIdempotent()
    {
        using var allocator = new InProcessAllocator();

        var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 1);

        var scope = NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100);
        scope.Dispose();
        scope.Dispose(); // second dispose should be a no-op

        var state = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
        await Assert.That(state.TibPointer).IsEqualTo(0u);

        NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
    }
}
