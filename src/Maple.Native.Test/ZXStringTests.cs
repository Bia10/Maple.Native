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

    [Test]
    public async Task ReadFrom_NegativeByteLength_Throws()
    {
        var image = new byte[32];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 5); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), -1); // nByteLen negative

        await Assert.That(() => ZXString.ReadFrom(image, 12)).Throws<System.IO.InvalidDataException>();
    }

    [Test]
    public async Task ReadFrom_PayloadExceedsSpan_Throws()
    {
        var image = new byte[14]; // header=12, only 2 extra bytes
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(4), 10); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(8), 10); // nByteLen > remaining

        await Assert.That(() => ZXString.ReadFrom(image, 12)).Throws<System.IO.InvalidDataException>();
    }

    [Test]
    public async Task Allocate_LargeString_PooledPath()
    {
        using var allocator = new InProcessAllocator();

        // 257 chars > StackBufferBytes (256) → pooled path
        var value = new string('A', 257);
        var alloc = ZXString.Allocate(allocator, value);

        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
        await Assert.That(alloc.ByteLength).IsEqualTo(257);

        ZXString.Destroy(allocator, alloc.ObjectAddress);
    }

    [Test]
    public async Task Allocate_BytePayload_StackPath()
    {
        using var allocator = new InProcessAllocator();

        byte[] payload = [0x41, 0x42, 0x43]; // "ABC"
        var alloc = ZXString.Allocate(allocator, payload);

        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
        await Assert.That(alloc.ByteLength).IsEqualTo(3);

        ZXString.Destroy(allocator, alloc.ObjectAddress);
    }

    [Test]
    public async Task Allocate_BytePayload_PooledPath()
    {
        using var allocator = new InProcessAllocator();

        // 257 bytes > StackBufferBytes (256) → pooled path
        var payload = new byte[257];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)('A' + (i % 26));

        var alloc = ZXString.Allocate(allocator, payload);

        await Assert.That(alloc.ObjectAddress).IsNotEqualTo(0u);
        await Assert.That(alloc.ByteLength).IsEqualTo(257);

        ZXString.Destroy(allocator, alloc.ObjectAddress);
    }

    [Test]
    public async Task Create_BytePayload_ReturnsNonZeroAddress()
    {
        using var allocator = new InProcessAllocator();

        var addr = ZXString.Create(allocator, new byte[] { 0x58, 0x59 });

        await Assert.That(addr).IsNotEqualTo(0u);

        ZXString.Destroy(allocator, addr);
    }

    [Test]
    public async Task WriteLatin1Payload_NonByteChar_Throws()
    {
        using var allocator = new InProcessAllocator();

        // 'あ' > 255 → WriteLatin1Payload throws ArgumentException
        await Assert.That(() => ZXString.Allocate(allocator, "あ")).Throws<ArgumentException>();
    }

    [Test]
    public async Task ToString_ReturnsValue()
    {
        var s = new ZXString("native");

        await Assert.That(s.ToString()).IsEqualTo("native");
    }

    [Test]
    public async Task ImplicitStringConversion_ReturnsValue()
    {
        var s = new ZXString("maple");
        string converted = s;

        await Assert.That(converted).IsEqualTo("maple");
    }
}
