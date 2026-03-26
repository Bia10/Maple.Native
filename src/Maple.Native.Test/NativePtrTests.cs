namespace Maple.Native.Test;

public class NativePtrTests
{
    [Test]
    public async Task Null_AddressIsZero()
    {
        var ptr = NativePtr<ZFatalSection>.Null;

        await Assert.That(ptr.Address).IsEqualTo(0u);
        await Assert.That(ptr.IsNull).IsTrue();
    }

    [Test]
    public async Task ImplicitCast_FromUInt()
    {
        NativePtr<ZFatalSection> ptr = 0x00401000u;

        await Assert.That(ptr.Address).IsEqualTo(0x00401000u);
        await Assert.That(ptr.IsNull).IsFalse();
    }

    [Test]
    public async Task ExplicitCast_ToUInt()
    {
        NativePtr<ZFatalSection> ptr = 0x00401234u;

        await Assert.That((uint)ptr).IsEqualTo(0x00401234u);
    }

    [Test]
    public async Task Add_OffsetsByBytes()
    {
        NativePtr<ZFatalSection> ptr = 0x00401000u;
        var shifted = ptr.Add(8);

        await Assert.That(shifted.Address).IsEqualTo(0x00401008u);
    }

    [Test]
    public async Task Equality_SameAddress_IsEqual()
    {
        NativePtr<ZFatalSection> a = 0x1000u;
        NativePtr<ZFatalSection> b = 0x1000u;

        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task IsInBounds_WithinImage_ReturnsTrue()
    {
        var imageBase = 0x00400000u;
        var view = new NativeImageView(new byte[0x1000], imageBase);
        NativePtr<ZFatalSection> ptr = imageBase + 0x100u;

        await Assert.That(ptr.IsInBounds(view)).IsTrue();
    }

    [Test]
    public async Task IsInBounds_OutsideImage_ReturnsFalse()
    {
        var imageBase = 0x00400000u;
        var view = new NativeImageView(new byte[0x1000], imageBase);
        NativePtr<ZFatalSection> ptr = 0x00500000u;

        await Assert.That(ptr.IsInBounds(view)).IsFalse();
    }

    [Test]
    public async Task Read_DereferencesCorrectly()
    {
        var imageBase = 0x00400000u;
        var imageBytes = new byte[0x100];
        var offset = 0x10;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(imageBytes.AsSpan(offset), 0xABCDu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(imageBytes.AsSpan(offset + 4), 1);

        var view = new NativeImageView(imageBytes, imageBase);
        NativePtr<ZFatalSection> ptr = imageBase + (uint)offset;

        var section = ptr.Read(view, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off));

        await Assert.That(section.TibPointer).IsEqualTo(0xABCDu);
        await Assert.That(section.RefCount).IsEqualTo(1);
    }
}
