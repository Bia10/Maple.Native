namespace Maple.Native.Test;

public class ZXStringTests
{
    [Test]
    public async Task Layout_TotalBytes_Is4()
    {
        int actual = ZXStringLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task DataLayout_HeaderBytes_Is12()
    {
        int actual = ZXStringDataLayout.HeaderBytes;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task DataLayout_PayloadOffset_Is12()
    {
        int actual = ZXStringDataLayout.PayloadOffset;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task DataLayout_TotalBytes_IncludesNullTerminator()
    {
        var layout = new ZXStringDataLayout(5);

        // Header(12) + payload(5) + null(1) = 18
        await Assert.That(layout.TotalBytes).IsEqualTo(18);
    }

    [Test]
    public async Task Constructor_SetsAllProperties()
    {
        var s = new ZXString("hello", refCount: 2, capacity: 16, byteLength: 5);

        await Assert.That(s.Value).IsEqualTo("hello");
        await Assert.That(s.RefCount).IsEqualTo(2);
        await Assert.That(s.Capacity).IsEqualTo(16);
        await Assert.That(s.ByteLength).IsEqualTo(5);
    }

    [Test]
    public async Task ReadFrom_DecodesAsciiString()
    {
        var payload = "hello"u8.ToArray();
        var image = new byte[ZXStringDataLayout.HeaderBytes + payload.Length + 1];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 1); // refCount
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 5); // capacity
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), 5); // byteLen
        payload.CopyTo(image.AsSpan(12));

        var result = ZXString.ReadFrom(image, 12);

        await Assert.That(result.Value).IsEqualTo("hello");
        await Assert.That(result.RefCount).IsEqualTo(1);
        await Assert.That(result.ByteLength).IsEqualTo(5);
    }

    [Test]
    public async Task AllocateAndRead_RoundTrip()
    {
        using var allocator = new InProcessAllocator();

        var alloc = ZXString.Allocate(allocator, "MapleStory");

        await Assert.That(alloc.ByteLength).IsEqualTo(10);
        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
        await Assert.That(alloc.DataAddress).IsNotEqualTo(0u);

        var headerBytes = ZXStringDataLayout.HeaderBytes;
        var raw = allocator.ReadBytes(alloc.DataAddress, headerBytes + alloc.ByteLength + 1);
        var readBack = ZXString.ReadFrom(raw, headerBytes);

        await Assert.That(readBack.Value).IsEqualTo("MapleStory");

        ZXString.Destroy(allocator, alloc.ObjectAddress);
    }

    [Test]
    public async Task Create_ReturnsSameAddressAsAllocate_ObjectAddress()
    {
        using var allocator = new InProcessAllocator();

        var addr = ZXString.Create(allocator, "test");

        await Assert.That(addr).IsNotEqualTo(0u);

        ZXString.Destroy(allocator, addr);
    }
}
