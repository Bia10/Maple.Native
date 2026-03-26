namespace Maple.Native.Test;

public class ZSocketTests
{
    // ── ZSocketBase ─────────────────────────────────────────────────────────

    [Test]
    public async Task SocketBaseLayout_TotalBytes_Is4()
    {
        int actual = ZSocketBaseLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(4);
    }

    [Test]
    public async Task SocketBase_NativeSize_Is4()
    {
        await Assert.That(ZSocketBase.NativeSize).IsEqualTo(4);
    }

    [Test]
    public async Task SocketBase_ReadFrom_DecodesHandle()
    {
        var bytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x000000BCu);

        var sock = ZSocketBase.ReadFrom(bytes, 0);

        await Assert.That(sock.SocketHandle).IsEqualTo(0x000000BCu);
    }

    [Test]
    public async Task SocketBase_Constructor_SetsHandle()
    {
        var sock = new ZSocketBase(0x000000BCu);

        await Assert.That(sock.SocketHandle).IsEqualTo(0x000000BCu);
    }

    // ── WsaBuf ──────────────────────────────────────────────────────────────

    [Test]
    public async Task WsaBufLayout_TotalBytes_Is8()
    {
        int actual = WsaBufLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(8);
    }

    [Test]
    public async Task WsaBufLayout_LenOffset_Is0()
    {
        int actual = WsaBufLayout.LenOffset;
        await Assert.That(actual).IsEqualTo(0);
    }

    [Test]
    public async Task WsaBufLayout_BufOffset_Is4()
    {
        int actual = WsaBufLayout.BufOffset;
        await Assert.That(actual).IsEqualTo(4);
    }

    // ── ZSocketBuffer ────────────────────────────────────────────────────────

    [Test]
    public async Task SocketBufferLayout_TotalBytes_Is28()
    {
        int actual = ZSocketBufferLayout.TotalBytes;
        await Assert.That(actual).IsEqualTo(28);
    }

    [Test]
    public async Task SocketBuffer_NativeSize_Is28()
    {
        await Assert.That(ZSocketBuffer.NativeSize).IsEqualTo(28);
    }

    [Test]
    public async Task SocketBuffer_ReadFrom_DecodesAllFields()
    {
        var bytes = new byte[28];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x00401000u); // vtbl
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 1); // refCount
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0u); // prev
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 1024u); // wsaLen
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 0x00403000u); // wsaBuf
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), 0x00404000u); // parent

        var buf = ZSocketBuffer.ReadFrom(bytes, 0);

        await Assert.That(buf.VTablePointer).IsEqualTo(0x00401000u);
        await Assert.That(buf.RefCount).IsEqualTo(1);
        await Assert.That(buf.WsaLen).IsEqualTo(1024u);
        await Assert.That(buf.WsaBuf).IsEqualTo(0x00403000u);
    }
}
