namespace Maple.Native.Test;

public class NativeCastTests
{
    [Test]
    public async Task SizeOf_ZFatalSection_Is8()
    {
        await Assert.That(NativeCast.SizeOf<ZFatalSection>()).IsEqualTo(8);
    }

    [Test]
    public async Task SizeOf_ZPair_Is8()
    {
        await Assert.That(NativeCast.SizeOf<ZPair>()).IsEqualTo(8);
    }

    [Test]
    public async Task NameOf_ReturnsTypeName()
    {
        await Assert.That(NativeCast.NameOf<ZFatalSection>()).IsEqualTo("ZFatalSection");
    }

    [Test]
    public async Task Reinterpret_OutOfProcess_DecodesStruct()
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0xDEADBEEFu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 3);

        var section = NativeCast.Reinterpret<ZFatalSection>(bytes, 0);

        await Assert.That(section.TibPointer).IsEqualTo(0xDEADBEEFu);
        await Assert.That(section.RefCount).IsEqualTo(3);
    }

    [Test]
    public async Task Is_WithSufficientBytes_ReturnsTrue()
    {
        var bytes = new byte[ZFatalSection.NativeSize + 4];

        await Assert.That(NativeCast.Is<ZFatalSection>(bytes, 0)).IsTrue();
    }

    [Test]
    public async Task Is_WithInsufficientBytes_ReturnsFalse()
    {
        var bytes = new byte[4]; // ZFatalSection needs 8

        await Assert.That(NativeCast.Is<ZFatalSection>(bytes, 0)).IsFalse();
    }

    [Test]
    public async Task As_ReturnsParsedValue_WhenBoundsOk()
    {
        var bytes = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x1234u);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 1);

        var result = NativeCast.As<ZFatalSection>(
            bytes,
            0,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off)
        );

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.TibPointer).IsEqualTo(0x1234u);
    }

    [Test]
    public async Task As_ReturnsNull_WhenOutOfBounds()
    {
        var bytes = new byte[4];

        // offset 4 is past the end of a 4-byte array → As returns null
        var result = NativeCast.As<ZFatalSection>(
            bytes,
            4,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off)
        );

        await Assert.That(result).IsNull();
    }
}
