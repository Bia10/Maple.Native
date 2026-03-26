namespace Maple.Native.Test;

public class ZXStringWideTests
{
    [Test]
    public async Task NullTerminatorBytes_Is2()
    {
        int actual = ZXStringWide.NullTerminatorBytes;
        await Assert.That(actual).IsEqualTo(2);
    }

    [Test]
    public async Task Constructor_SetsAllProperties()
    {
        var s = new ZXStringWide("hello", refCount: 1, capacity: 5, byteLength: 10);

        await Assert.That(s.Value).IsEqualTo("hello");
        await Assert.That(s.RefCount).IsEqualTo(1);
        await Assert.That(s.Capacity).IsEqualTo(5);
        await Assert.That(s.ByteLength).IsEqualTo(10);
    }

    [Test]
    public async Task CharCount_IsByteLength_DividedBy2()
    {
        var s = new ZXStringWide("hi", byteLength: 4);

        await Assert.That(s.CharCount).IsEqualTo(2);
    }

    [Test]
    public async Task Allocate_RoundTrip()
    {
        using var allocator = new InProcessAllocator();

        var alloc = ZXStringWide.Allocate(allocator, "Maple");

        // "Maple" = 5 chars × 2 bytes = 10 byteLen
        await Assert.That(alloc.ByteLength).IsEqualTo(10);
        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);

        ZXStringWide.Destroy(allocator, alloc.ObjectAddress);
    }

    [Test]
    public async Task Create_ReturnsNonZeroAddress()
    {
        using var allocator = new InProcessAllocator();

        var addr = ZXStringWide.Create(allocator, "hello");

        await Assert.That(addr).IsNotEqualTo(0u);

        ZXStringWide.Destroy(allocator, addr);
    }
}
