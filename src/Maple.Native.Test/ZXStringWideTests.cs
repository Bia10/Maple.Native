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

    [Test]
    public async Task ReadFrom_RoundTrip()
    {
        // Build a flat byte buffer containing a ZXStringWide header + UTF-16LE payload
        // Header at [0]: nRef=2, nCap=3, nByteLen=6 (3 wchars)
        // Payload at [12]: "abc" in UTF-16LE
        var headerBytes = ZXStringDataLayout.HeaderBytes; // 12
        var payloadBytes = 6; // 3 chars × 2
        var image = new byte[headerBytes + payloadBytes + 2]; // +2 null terminator
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 2); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 3); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), 6); // nByteLen
        image[12] = 0x61; // 'a'
        image[13] = 0x00;
        image[14] = 0x62; // 'b'
        image[15] = 0x00;
        image[16] = 0x63; // 'c'
        image[17] = 0x00;

        var result = ZXStringWide.ReadFrom(image, 12);

        await Assert.That(result.Value).IsEqualTo("abc");
        await Assert.That(result.RefCount).IsEqualTo(2);
        await Assert.That(result.Capacity).IsEqualTo(3);
        await Assert.That(result.ByteLength).IsEqualTo(6);
        await Assert.That(result.CharCount).IsEqualTo(3);
    }

    [Test]
    public async Task ReadFrom_NegativeOffset_Throws()
    {
        var image = new byte[32];

        await Assert.That(() => ZXStringWide.ReadFrom(image, -1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ReadFrom_TooSmallOffset_Throws()
    {
        var image = new byte[32];

        // offset < HeaderBytes (12) → throws
        await Assert.That(() => ZXStringWide.ReadFrom(image, 8)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ReadFrom_OffsetExceedsImageRange_Throws()
    {
        // image.Length = 12 (exactly HeaderBytes), payloadFileOffset = 13
        // headerBase = 13 - 12 = 1 > 12 - 12 = 0 → throws
        var image = new byte[12];

        await Assert.That(() => ZXStringWide.ReadFrom(image, 13)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ReadFrom_NegativeByteLength_Throws()
    {
        var image = new byte[32];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 1); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), -2); // nByteLen negative

        await Assert.That(() => ZXStringWide.ReadFrom(image, 12)).Throws<System.IO.InvalidDataException>();
    }

    [Test]
    public async Task ReadFrom_OddByteLength_Throws()
    {
        var image = new byte[32];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 1); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), 3); // nByteLen odd

        await Assert.That(() => ZXStringWide.ReadFrom(image, 12)).Throws<System.IO.InvalidDataException>();
    }

    [Test]
    public async Task ReadFrom_PayloadExceedsSpan_Throws()
    {
        var image = new byte[14]; // header=12, only 2 extra bytes
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 5); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), 10); // nByteLen > remaining

        await Assert.That(() => ZXStringWide.ReadFrom(image, 12)).Throws<System.IO.InvalidDataException>();
    }

    [Test]
    public async Task Allocate_LargeString_PooledPath()
    {
        using var allocator = new InProcessAllocator();

        // 250 chars × 2 bytes = 500 payload bytes.
        // GetAllocationBytes = 12 + 500 + 2 = 514 > StackBufferBytes (512) → pooled path
        var value = new string('X', 250);
        var alloc = ZXStringWide.Allocate(allocator, value);

        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
        await Assert.That(alloc.ByteLength).IsEqualTo(500);
        await Assert.That(alloc.Capacity).IsEqualTo(250);

        ZXStringWide.Destroy(allocator, alloc.ObjectAddress);
    }

    [Test]
    public async Task ToString_ReturnsValue()
    {
        var s = new ZXStringWide("test");

        await Assert.That(s.ToString()).IsEqualTo("test");
    }

    [Test]
    public async Task ImplicitStringConversion_ReturnsValue()
    {
        var s = new ZXStringWide("world");
        string converted = s;

        await Assert.That(converted).IsEqualTo("world");
    }
}
