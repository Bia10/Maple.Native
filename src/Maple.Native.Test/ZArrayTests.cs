namespace Maple.Native.Test;

public class ZArrayTests
{
    [Test]
    public async Task Layout_HeaderBytes_Is4()
    {
        int actual = ZArrayLayout.HeaderBytes;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Layout_PayloadOffset_Is4()
    {
        int actual = ZArrayLayout.PayloadOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Layout_CountOffset_Is0()
    {
        int actual = ZArrayLayout.CountOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task Layout_ElementCount_ReflectsConstructorArg()
    {
        var layout = new ZArrayLayout(42);

        await Assert.That(layout.ElementCount).IsEqualTo(42);
    }

    [Test]
    public async Task ReadCount_DecodesCorrectly()
    {
        var image = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 3);

        // payloadFileOffset points to one-past-header (offset 4)
        var count = ZArray.ReadCount(image, 4);

        await Assert.That(count).IsEqualTo(3);
    }

    [Test]
    public async Task ReadByteElements_ReturnsCorrectBytes()
    {
        var image = new byte[10];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 2);
        image[4] = 0xAB;
        image[5] = 0xCD;

        var elements = ZArray.ReadByteElements(image, 4, 2);

        await Assert.That(elements.Length).IsEqualTo(2);
        await Assert.That(elements[0]).IsEqualTo((byte)0xAB);
        await Assert.That(elements[1]).IsEqualTo((byte)0xCD);
    }

    [Test]
    public async Task ReadPointerElements_Returns4ByteUInts()
    {
        var image = new byte[12];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(4), 0x00401000u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(8), 0x00402000u);

        var elements = ZArray.ReadPointerElements(image, 4, 2);

        await Assert.That(elements.Length).IsEqualTo(2);
        await Assert.That(elements[0]).IsEqualTo(0x00401000u);
        await Assert.That(elements[1]).IsEqualTo(0x00402000u);
    }

    [Test]
    public async Task Allocate_RoundTrip_ByteElements()
    {
        using var allocator = new InProcessAllocator();

        byte[] data = [0x01, 0x02, 0x03];
        var alloc = ZArray<byte>.Allocate(allocator, data);

        await Assert.That(alloc.ElementCount).IsEqualTo(3);
        await Assert.That(alloc.PayloadAddress).IsNotEqualTo(0u);

        var raw = allocator.ReadBytes(alloc.BaseAddress, ZArrayLayout.HeaderBytes + 3);
        int count = ZArray.ReadCount(raw, ZArrayLayout.PayloadOffset);
        var elements = ZArray.ReadByteElements(raw, ZArrayLayout.PayloadOffset, count);

        await Assert.That(elements[0]).IsEqualTo((byte)0x01);
        await Assert.That(elements[1]).IsEqualTo((byte)0x02);
        await Assert.That(elements[2]).IsEqualTo((byte)0x03);

        ZArray<byte>.Destroy(allocator, alloc.PayloadAddress);
    }
}
