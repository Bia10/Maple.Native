namespace Maple.Native.Test;

public class ZMapTests
{
    [Test]
    public async Task Layout_TotalBytes_Is24()
    {
        int actual = ZMapLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(24);
    }

    [Test]
    public async Task Layout_TableOffset_Is4()
    {
        int actual = ZMapLayout.TableOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task Layout_CountOffset_Is12()
    {
        int actual = ZMapLayout.CountOffset;
        await Assert.That(actual).IsEqualTo(12);
    }

    [Test]
    public async Task PairLayout_IntInt_Is16Bytes()
    {
        await Assert.That(ZMapPairLayout.IntInt.TotalBytes).IsEqualTo(16);
    }

    [Test]
    public async Task PairLayout_XStringZPair_Is20Bytes()
    {
        await Assert.That(ZMapPairLayout.XStringZPair.TotalBytes).IsEqualTo(20);
    }

    [Test]
    public async Task PairLayout_ValueOffset_IsKeyOffsetPlusKeyBytes()
    {
        var layout = new ZMapPairLayout(keyBytes: 4, valueBytes: 8);

        await Assert.That(layout.ValueOffset).IsEqualTo(ZMapPairLayout.KeyOffset + 4);
    }

    [Test]
    public async Task ReadHeader_DecodesAllFields()
    {
        var bytes = new byte[ZMapLayout.TotalBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(ZMapLayout.TableOffset),
            0x00401000u
        );
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ZMapLayout.TableSizeOffset), 64u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(ZMapLayout.CountOffset), 3u);

        var header = ZMapHeader.ReadFrom(bytes, 0);

        await Assert.That(header.TableVa).IsEqualTo(0x00401000u);
        await Assert.That(header.TableSize).IsEqualTo(64u);
        await Assert.That(header.Count).IsEqualTo(3u);
    }

    [Test]
    public async Task ReadBucketHead_ReturnsPointerAtBucketIndex()
    {
        var table = new byte[12];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(0), 0u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(4), 0x00402000u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(table.AsSpan(8), 0u);

        var head = ZMap.ReadBucketHead(table, 0, bucketIndex: 1);

        await Assert.That(head).IsEqualTo(0x00402000u);
    }

    [Test]
    public async Task ReadPairNext_ReturnsNextNodePointer()
    {
        // _PAIR node: 4-byte vtbl at 0x00, 4-byte pNext at ZMapPairLayout.NextOffset (=4)
        var node = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(node.AsSpan(0), 0xDEADu); // vtbl
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            node.AsSpan(ZMapPairLayout.NextOffset),
            0x00403000u
        );

        var next = ZMap.ReadPairNext(node, 0);

        await Assert.That(next).IsEqualTo(0x00403000u);
    }

    [Test]
    public async Task ReadHeader_AutoGrowFields()
    {
        var bytes = new byte[ZMapLayout.TotalBytes];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(ZMapLayout.AutoGrowEvery128Offset),
            128u
        );
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(ZMapLayout.AutoGrowLimitOffset),
            192u
        );

        var header = ZMapHeader.ReadFrom(bytes, 0);

        await Assert.That(header.AutoGrowEvery128).IsEqualTo(128u);
        await Assert.That(header.AutoGrowLimit).IsEqualTo(192u);
    }

    [Test]
    public async Task Layout_VTableOffset_Is0()
    {
        int actual = ZMapLayout.VTableOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task Layout_TableSizeOffset_Is8()
    {
        int actual = ZMapLayout.TableSizeOffset;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task PairLayout_VTableOffset_Is0()
    {
        int actual = ZMapPairLayout.VTableOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task PairLayout_NextOffset_Is4()
    {
        int actual = ZMapPairLayout.NextOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task PairLayout_KeyOffset_Is8()
    {
        int actual = ZMapPairLayout.KeyOffset;
        await Assert.That(actual).IsEqualTo(8);
    }
}
