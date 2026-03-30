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

    [Test]
    public async Task TryCast_InBounds_ReturnsTrue()
    {
        var bytes = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0xDEADu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 2);

        var ok = NativeCast.TryCast<ZFatalSection>(
            bytes,
            0,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off),
            out var result
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(result.TibPointer).IsEqualTo(0xDEADu);
    }

    [Test]
    public async Task TryCast_NegativeOffset_ReturnsFalse()
    {
        var bytes = new byte[8];

        var ok = NativeCast.TryCast<ZFatalSection>(
            bytes,
            -1,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off),
            out _
        );

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task TryCast_OffsetPastEnd_ReturnsFalse()
    {
        var bytes = new byte[8];

        var ok = NativeCast.TryCast<ZFatalSection>(
            bytes,
            8,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off),
            out _
        );

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task Is_NegativeOffset_ReturnsFalse()
    {
        var bytes = new byte[16];

        await Assert.That(NativeCast.Is<ZFatalSection>(bytes, -1)).IsFalse();
    }

    [Test]
    public async Task SafeRead_ZeroPointer_ReturnsFalseWithDefault()
    {
        bool ok;
        int result;
        unsafe
        {
            ok = NativeCast.SafeRead<int>((nint)0, out result);
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task SafeRead_ValidPointer_ReturnsTrue()
    {
        bool ok;
        int result = 0;
        unsafe
        {
            int[] arr = new int[] { 42 };
            fixed (int* p = arr)
            {
                ok = NativeCast.SafeRead<int>((nint)p, out result);
            }
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Reinterpret_InProcess_ReadsValue()
    {
        int result;
        unsafe
        {
            int[] arr = new int[] { 0x12345678 };
            fixed (int* p = arr)
            {
                result = NativeCast.Reinterpret<int>((nint)p);
            }
        }

        await Assert.That(result).IsEqualTo(0x12345678);
    }
}
