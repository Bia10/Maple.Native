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

    [Test]
    public async Task FileOffset_BelowImageBase_Throws()
    {
        var view = MakeView(new byte[0x1000]);

        await Assert.That(() => view.FileOffset(0x00300000u)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task FileOffset_BeyondImageSize_Throws()
    {
        var view = MakeView(new byte[0x100]);

        await Assert.That(() => view.FileOffset(ImageBase + 0x200u)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TryCast_NullAddress_ReturnsFalse()
    {
        var view = MakeView(new byte[0x100]);

        var ok = view.TryCast<ZFatalSection>(0u, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off), out _);

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task TryCast_BelowImageBase_ReturnsFalse()
    {
        var view = MakeView(new byte[0x100]);

        var ok = view.TryCast<ZFatalSection>(
            0x00300000u,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off),
            out _
        );

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task TryCast_InBounds_ReturnsTrue()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x10), 0x1111u);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x14), 7);

        var view = MakeView(bytes);
        var ok = view.TryCast<ZFatalSection>(
            ImageBase + 0x10u,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off),
            out var section
        );

        await Assert.That(ok).IsTrue();
        await Assert.That(section.TibPointer).IsEqualTo(0x1111u);
    }

    [Test]
    public async Task Cast_NativePtr_Overload_DecodesCorrectly()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x10), 0xBEEFu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x14), 3);

        var view = MakeView(bytes);
        NativePtr<ZFatalSection> ptr = ImageBase + 0x10u;
        var section = view.Cast(ptr, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off));

        await Assert.That(section.TibPointer).IsEqualTo(0xBEEFu);
    }

    [Test]
    public async Task TryCast_NativePtr_Success()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x20), 0xCAFEu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x24), 1);

        var view = MakeView(bytes);
        NativePtr<ZFatalSection> ptr = ImageBase + 0x20u;
        var ok = view.TryCast(ptr, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off), out var section);

        await Assert.That(ok).IsTrue();
        await Assert.That(section.TibPointer).IsEqualTo(0xCAFEu);
    }

    [Test]
    public async Task TryCast_NativePtr_Null_ReturnsFalse()
    {
        var view = MakeView(new byte[0x100]);
        NativePtr<ZFatalSection> ptr = 0u;

        var ok = view.TryCast(ptr, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off), out _);

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task Contains_NullAddress_ReturnsFalse()
    {
        var view = MakeView(new byte[0x100]);

        await Assert.That(view.Contains(0u)).IsFalse();
    }

    [Test]
    public async Task Contains_BelowImageBase_ReturnsFalse()
    {
        var view = MakeView(new byte[0x100]);

        await Assert.That(view.Contains(0x00200000u)).IsFalse();
    }

    [Test]
    public async Task Is_NativePtr_Overload_ReturnsTrue()
    {
        var view = MakeView(new byte[0x100]);
        NativePtr<ZFatalSection> ptr = ImageBase + 0x10u;

        await Assert.That(view.Is<ZFatalSection>(ptr)).IsTrue();
    }

    [Test]
    public async Task As_NativePtr_ValidAddress_ReturnsValue()
    {
        var bytes = new byte[0x100];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x30), 0xAAAAu);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x34), 5);

        var view = MakeView(bytes);
        NativePtr<ZFatalSection> ptr = ImageBase + 0x30u;
        var result = view.As(ptr, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off));

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.TibPointer).IsEqualTo(0xAAAAu);
    }

    [Test]
    public async Task As_NativePtr_NullAddress_ReturnsNull()
    {
        var view = MakeView(new byte[0x100]);
        NativePtr<ZFatalSection> ptr = 0u;

        var result = view.As(ptr, (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off));

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReadZArrayOfPtrs_ExplicitCount_DecodesElements()
    {
        var bytes = new byte[0x100];

        // ZFatalSection structs at offsets 0x10 and 0x20
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x10), 0x1111u);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x14), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x20), 0x2222u);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x24), 2);

        // ZArray ptrs at offset 0x40 (payload VA = ImageBase+0x40): [ptr0, ptr1]
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x40), ImageBase + 0x10u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x44), ImageBase + 0x20u);

        var view = MakeView(bytes);
        var result = view.ReadZArrayOfPtrs<ZFatalSection>(
            ImageBase + 0x40u,
            2,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off)
        );

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].TibPointer).IsEqualTo(0x1111u);
        await Assert.That(result[1].TibPointer).IsEqualTo(0x2222u);
    }

    [Test]
    public async Task ReadZArrayOfPtrs_AutoCount_DecodesElements()
    {
        var bytes = new byte[0x100];

        // ZFatalSection structs at offsets 0x10 and 0x20
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x10), 0x3333u);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x14), 3);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x20), 0x4444u);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x24), 4);

        // ZArray: count header at 0x3C (=payload-4), payload at 0x40
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x40), ImageBase + 0x10u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x44), ImageBase + 0x20u);

        var view = MakeView(bytes);
        var result = view.ReadZArrayOfPtrs<ZFatalSection>(
            ImageBase + 0x40u,
            (img, off) => NativeCast.Reinterpret<ZFatalSection>(img, off)
        );

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].TibPointer).IsEqualTo(0x3333u);
        await Assert.That(result[1].TibPointer).IsEqualTo(0x4444u);
    }

    // ── ZXString array helpers ────────────────────────────────────────────────

    // Image layout for ZXString array tests (0x100 bytes):
    //   [0x010] ZXString object 1: _m_pStr = ImageBase+0x060
    //   [0x014] ZXString object 2: _m_pStr = ImageBase+0x074
    //   ZXStringDataLayout.HeaderBytes = 12
    //   [0x054] nRef=1, [0x058] nCap=2, [0x05C] nByteLen=2 → payload "hi"
    //   [0x060] 'h'=0x68, [0x061] 'i'=0x69
    //   [0x068] nRef=1, [0x06C] nCap=2, [0x070] nByteLen=2 → payload "ab"
    //   [0x074] 'a'=0x61, [0x075] 'b'=0x62
    //   ZArray count at [0x0A4]=1, payload at [0x0A8]: ptr[0]=ImageBase+0x010
    //   ZArray count at [0x0B4]=2, payload at [0x0B8]: ptr[0..1]

    static byte[] BuildZXStringImage()
    {
        var bytes = new byte[0x100];

        // ZXString object 1: _m_pStr = ImageBase+0x060
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x010), ImageBase + 0x060u);

        // ZXString object 2: _m_pStr = ImageBase+0x074
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x014), ImageBase + 0x074u);

        // Header for ZXString 1 (payload at 0x060 → header at 0x054)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x054), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x058), 2); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x05C), 2); // nByteLen
        bytes[0x060] = (byte)'h';
        bytes[0x061] = (byte)'i';

        // Header for ZXString 2 (payload at 0x074 → header at 0x068)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x068), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x06C), 2); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x070), 2); // nByteLen
        bytes[0x074] = (byte)'a';
        bytes[0x075] = (byte)'b';

        // ZArray explicit-count: payload at 0x0A8 (1 element)
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x0A8), ImageBase + 0x010u);

        // ZArray auto-count: count at 0x0B4, payload at 0x0B8 (2 elements)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x0B4), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x0B8), ImageBase + 0x010u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x0BC), ImageBase + 0x014u);

        return bytes;
    }

    [Test]
    public async Task ReadZArrayOfZXString_ExplicitCount_DecodesElements()
    {
        var bytes = BuildZXStringImage();
        var view = MakeView(bytes);

        var result = view.ReadZArrayOfZXString(ImageBase + 0x0A8u, 1);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Value).IsEqualTo("hi");
    }

    [Test]
    public async Task ReadZArrayOfZXString_AutoCount_DecodesElements()
    {
        var bytes = BuildZXStringImage();
        var view = MakeView(bytes);

        var result = view.ReadZArrayOfZXString(ImageBase + 0x0B8u);

        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].Value).IsEqualTo("hi");
        await Assert.That(result[1].Value).IsEqualTo("ab");
    }

    // ── ZXStringWide array helpers ────────────────────────────────────────────

    // Image layout for ZXStringWide array tests (0x100 bytes):
    //   [0x010] ZXStringWide object: _m_pStr = ImageBase+0x060
    //   ZXStringDataLayout.HeaderBytes = 12
    //   [0x054] nRef=1, [0x058] nCap=1, [0x05C] nByteLen=2 (1 wchar)
    //   [0x060] 'A' UTF-16LE: 0x41, 0x00
    //   ZArray count at [0x0A4]=1, payload at [0x0A8]: ptr[0]=ImageBase+0x010
    //   ZArray auto-count at [0x0B4]=1, payload at [0x0B8]

    static byte[] BuildZXStringWideImage()
    {
        var bytes = new byte[0x100];

        // ZXStringWide object: _m_pStr = ImageBase+0x060
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x010), ImageBase + 0x060u);

        // Header (payload at 0x060 → header at 0x054)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x054), 1); // nRef
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x058), 1); // nCap
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x05C), 2); // nByteLen (1 wchar × 2)
        bytes[0x060] = 0x41; // 'A' UTF-16LE low byte
        bytes[0x061] = 0x00; // 'A' UTF-16LE high byte

        // ZArray explicit-count: payload at 0x0A8 (1 element)
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x0A8), ImageBase + 0x010u);

        // ZArray auto-count: count at 0x0B4, payload at 0x0B8
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x0B4), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x0B8), ImageBase + 0x010u);

        return bytes;
    }

    [Test]
    public async Task ReadZArrayOfZXStringWide_ExplicitCount_DecodesElements()
    {
        var bytes = BuildZXStringWideImage();
        var view = MakeView(bytes);

        var result = view.ReadZArrayOfZXStringWide(ImageBase + 0x0A8u, 1);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Value).IsEqualTo("A");
    }

    [Test]
    public async Task ReadZArrayOfZXStringWide_AutoCount_DecodesElements()
    {
        var bytes = BuildZXStringWideImage();
        var view = MakeView(bytes);

        var result = view.ReadZArrayOfZXStringWide(ImageBase + 0x0B8u);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0].Value).IsEqualTo("A");
    }
}
