namespace Maple.Native.Test;

public class NativeImageViewTests
{
    static readonly uint ImageBase = 0x00400000u;

    static NativeImageView MakeView(byte[] bytes) => new(bytes, ImageBase);

    [Test]
    public async Task FileOffset_ConvertsVaCorrectly()
    {
        var view = MakeView(new byte[0x1000]);

        await Assert.That(view.FileOffset(ImageBase + 0x100u)).IsEqualTo(0x100);
    }

    [Test]
    public async Task Contains_InsideImage_ReturnsTrue()
    {
        var view = MakeView(new byte[0x1000]);

        await Assert.That(view.Contains(ImageBase + 0x100u)).IsTrue();
    }

    [Test]
    public async Task Contains_OutsideImage_ReturnsFalse()
    {
        var view = MakeView(new byte[0x1000]);

        await Assert.That(view.Contains(0x00500000u)).IsFalse();
    }

    [Test]
    public async Task Is_SizedType_TrueWhenFits()
    {
        var view = MakeView(new byte[0x100]);

        await Assert.That(view.Is<ZFatalSection>(ImageBase)).IsTrue();
    }

    [Test]
    public async Task Cast_ZFatalSection_DecodesCorrectly()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x10), 0xABCDu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x14), 2);

        var view = MakeView(bytes);
        var section = view.Cast<ZFatalSection>(
            ImageBase + 0x10u,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off)
        );

        await Assert.That(section.TibPointer).IsEqualTo(0xABCDu);
        await Assert.That(section.RefCount).IsEqualTo(2);
    }

    [Test]
    public async Task TryCast_OutOfBounds_ReturnsFalse()
    {
        var view = MakeView(new byte[0x100]);

        var ok = view.TryCast<ZFatalSection>(
            0x00500000u,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off),
            out _
        );

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task As_ReturnsNull_WhenOutOfBounds()
    {
        var view = MakeView(new byte[0x100]);

        var result = view.As<ZFatalSection>(0x00500000u, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off));

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Cast_ZPair_DecodesAtOffset()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x20), 11);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x24), 22);

        var view = MakeView(bytes);
        var pair = view.Cast<ZPair>(ImageBase + 0x20u, ZPair.ReadFrom);

        await Assert.That(pair.First).IsEqualTo(11);
        await Assert.That(pair.Second).IsEqualTo(22);
    }

    [Test]
    public async Task Cast_ZThread_DecodesAtOffset()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x30), 0x00401000u); // vtbl
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x34), 0x5678u); // threadId
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x38), 0x9ABCu); // handle

        var view = MakeView(bytes);
        var thread = view.Cast<ZThread>(ImageBase + 0x30u, ZThread.ReadFrom);

        await Assert.That(thread.ThreadId).IsEqualTo(0x5678u);
        await Assert.That(thread.ThreadHandle).IsEqualTo(0x9ABCu);
    }
}
