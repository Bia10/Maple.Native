using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>_WSABUF</c> (Winsock scatter/gather buffer descriptor):
/// <code>
/// struct _WSABUF {
///     unsigned int  len;   // +0x00  byte length of the buffer
///     char         *buf;   // +0x04  pointer to the buffer
/// };  // sizeof = 0x08
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 21805.
/// </remarks>
public readonly ref struct WsaBufLayout
{
    /// <summary>Byte offset of <c>len</c> — buffer byte length.</summary>
    public const int LenOffset = 0;

    /// <summary>Byte offset of <c>buf</c> — buffer data pointer.</summary>
    public const int BufOffset = TypeSizes.Int32;

    /// <summary>Total struct size in bytes (8).</summary>
    public const int TotalBytes = BufOffset + TypeSizes.Pointer;
}

/// <summary>
/// Memory layout of <c>ZSocketBuffer</c>:
/// <code>
/// struct ZSocketBuffer : ZRefCounted, _WSABUF, ZRefCountedAccessorBase {
///     // ZRefCounted base (EBO does not apply — has vtbl):
///     //   vftable     (+0x00, 4 bytes)
///     //   nRef/pNext  (+0x04, 4 bytes)  ← union: ref count when live, free-list ptr when recycled
///     //   pPrev       (+0x08, 4 bytes)
///     // _WSABUF embedded:
///     //   len         (+0x0C, 4 bytes)
///     //   buf         (+0x10, 4 bytes)
///     // ZRefCountedAccessorBase: empty struct — EBO removes it entirely
///     ZRef&lt;ZSocketBuffer&gt; _m_pParent;   // +0x14  gap(4) + p(4) = 8 bytes
/// };  // sizeof = 0x1C
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 21817.
/// </remarks>
public readonly ref struct ZSocketBufferLayout
{
    /// <summary>Byte offset of <c>__vftable</c> (inherited from <c>ZRefCounted</c>).</summary>
    public const int VTableOffset = ZRefCountedLayout.VTableOffset;

    /// <summary>Byte offset of <c>_m_nRef</c>/<c>_m_pNext</c> (inherited from <c>ZRefCounted</c>).</summary>
    public const int RefCountOffset = ZRefCountedLayout.RefCountOffset;

    /// <summary>Byte offset of <c>_m_pPrev</c> (inherited from <c>ZRefCounted</c>).</summary>
    public const int PrevOffset = ZRefCountedLayout.PrevOffset;

    /// <summary>Byte offset of <c>_WSABUF::len</c> — buffer byte length.</summary>
    public const int WsaLenOffset = ZRefCountedLayout.TotalBytes + WsaBufLayout.LenOffset;

    /// <summary>Byte offset of <c>_WSABUF::buf</c> — buffer data pointer.</summary>
    public const int WsaBufOffset = ZRefCountedLayout.TotalBytes + WsaBufLayout.BufOffset;

    /// <summary>Byte offset of <c>_m_pParent</c>'s inner pointer (<c>ZRef.p</c>).</summary>
    public const int ParentPointerOffset =
        ZRefCountedLayout.TotalBytes + WsaBufLayout.TotalBytes + ZRefLayout.PointerOffset;

    /// <summary>Total struct size in bytes (0x1C = 28).</summary>
    public const int TotalBytes =
        ZRefCountedLayout.TotalBytes // 0x0C
        + WsaBufLayout.TotalBytes // +0x08 = 0x14
        + ZRefLayout.TotalBytes; // +0x08 = 0x1C
}

/// <summary>
/// Mirrors <c>ZSocketBuffer</c>.
/// </summary>
public readonly struct ZSocketBuffer : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => ZSocketBufferLayout.TotalBytes;

    /// <summary>Virtual function table pointer (from <c>ZRefCounted</c> base).</summary>
    public uint VTablePointer { get; }

    /// <summary>Reference count (from <c>ZRefCounted._m_nRef</c>).</summary>
    public int RefCount { get; }

    /// <summary>Allocator list prev pointer (from <c>ZRefCounted._m_pPrev</c>).</summary>
    public uint PrevPointer { get; }

    /// <summary>Buffer byte length (<c>_WSABUF::len</c>).</summary>
    public uint WsaLen { get; }

    /// <summary>Buffer data pointer (<c>_WSABUF::buf</c>).</summary>
    public uint WsaBuf { get; }

    /// <summary>Inner pointer of <c>_m_pParent</c> (<c>ZRef&lt;ZSocketBuffer&gt;::p</c>).</summary>
    public uint ParentPointer { get; }

    /// <summary>Creates a <see cref="ZSocketBuffer"/> with the specified fields.</summary>
    public ZSocketBuffer(
        uint vTablePointer,
        int refCount,
        uint prevPointer,
        uint wsaLen,
        uint wsaBuf,
        uint parentPointer
    )
    {
        VTablePointer = vTablePointer;
        RefCount = refCount;
        PrevPointer = prevPointer;
        WsaLen = wsaLen;
        WsaBuf = wsaBuf;
        ParentPointer = parentPointer;
    }

    /// <summary>Reads a <c>ZSocketBuffer</c> from binary data at <paramref name="fileOffset"/>.</summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="fileOffset">File offset of the struct start.</param>
    public static ZSocketBuffer ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZSocketBufferLayout.VTableOffset)..]),
            BinaryPrimitives.ReadInt32LittleEndian(image[(fileOffset + ZSocketBufferLayout.RefCountOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZSocketBufferLayout.PrevOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZSocketBufferLayout.WsaLenOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZSocketBufferLayout.WsaBufOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZSocketBufferLayout.ParentPointerOffset)..])
        );
}
