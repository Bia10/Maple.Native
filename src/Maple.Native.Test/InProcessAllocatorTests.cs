namespace Maple.Native.Test;

public class InProcessAllocatorTests
{
    [Test]
    public async Task Allocate_ReturnsNonZeroAddress()
    {
        using var allocator = new InProcessAllocator();

        var addr = allocator.Allocate(16);

        await Assert.That(addr).IsNotEqualTo(0u);
    }

    [Test]
    public async Task WriteAndRead_RoundTrip()
    {
        using var allocator = new InProcessAllocator();

        var addr = allocator.Allocate(4);
        allocator.Write(addr, [0x01, 0x02, 0x03, 0x04]);

        var result = allocator.ReadBytes(addr, 4);

        await Assert.That(result[0]).IsEqualTo((byte)0x01);
        await Assert.That(result[1]).IsEqualTo((byte)0x02);
        await Assert.That(result[2]).IsEqualTo((byte)0x03);
        await Assert.That(result[3]).IsEqualTo((byte)0x04);
    }

    [Test]
    public async Task ReadUInt32_ReturnsWrittenValue()
    {
        using var allocator = new InProcessAllocator();

        var addr = allocator.Allocate(4);
        var bytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0xDEADBEEFu);
        allocator.Write(addr, bytes);

        var value = allocator.ReadUInt32(addr);

        await Assert.That(value).IsEqualTo(0xDEADBEEFu);
    }

    [Test]
    public async Task Free_DoesNotThrow()
    {
        using var allocator = new InProcessAllocator();

        var addr = allocator.Allocate(8);

        await Assert.That(() => allocator.Free(addr)).ThrowsNothing();
    }

    [Test]
    public async Task Read_ReturnsFalse_ForUnallocatedAddress()
    {
        using var allocator = new InProcessAllocator();

        var result = allocator.Read(0xDEAD0000u, new byte[4]);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CompareExchangeUInt32_Succeeds_WhenExpectedMatches()
    {
        using var allocator = new InProcessAllocator();

        var addr = allocator.Allocate(4);
        var bytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes, 1u);
        allocator.Write(addr, bytes);

        var success = allocator.CompareExchangeUInt32(addr, expected: 1u, desired: 2u, out var observed);

        await Assert.That(success).IsTrue();
        await Assert.That(observed).IsEqualTo(1u);
        await Assert.That(allocator.ReadUInt32(addr)).IsEqualTo(2u);
    }

    [Test]
    public async Task CompareExchangeUInt32_Fails_WhenExpectedMismatch()
    {
        using var allocator = new InProcessAllocator();

        var addr = allocator.Allocate(4);
        var bytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes, 5u);
        allocator.Write(addr, bytes);

        var success = allocator.CompareExchangeUInt32(addr, expected: 99u, desired: 0u, out var observed);

        await Assert.That(success).IsFalse();
        await Assert.That(observed).IsEqualTo(5u);
        await Assert.That(allocator.ReadUInt32(addr)).IsEqualTo(5u); // unchanged
    }

    [Test]
    public async Task CurrentThreadTeb_IsNonZero()
    {
        using var allocator = new InProcessAllocator();

        await Assert.That(allocator.CurrentThreadTeb).IsNotEqualTo(0u);
    }
}
