using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Memory layout of <c>ZThread</c>:
/// <code>
/// struct ZThread {
///     ZThread_vtbl  *__vftable;       // +0x00  virtual function table pointer
///     unsigned int   _m_dwThreadId;   // +0x04  Win32 thread identifier
///     void          *_m_hThread;      // +0x08  Win32 thread handle
/// };  // sizeof = 0x0C
/// </code>
/// </summary>
/// <remarks>
/// PDB source: line 34416.
/// </remarks>
public readonly ref struct ZThreadLayout
{
    /// <summary>Byte offset of the virtual function table pointer.</summary>
    public const int VTableOffset = 0;

    /// <summary>Byte offset of <c>_m_dwThreadId</c> — Win32 thread identifier.</summary>
    public const int ThreadIdOffset = TypeSizes.Pointer;

    /// <summary>Byte offset of <c>_m_hThread</c> — Win32 thread handle.</summary>
    public const int ThreadHandleOffset = ThreadIdOffset + TypeSizes.Int32;

    /// <summary>Total struct size in bytes (0x0C = 12).</summary>
    public const int TotalBytes = ThreadHandleOffset + TypeSizes.Pointer;
}

/// <summary>
/// Mirrors <c>ZThread</c>.
/// </summary>
public readonly struct ZThread : INativeSized
{
    /// <inheritdoc/>
    public static int NativeSize => ZThreadLayout.TotalBytes;

    /// <summary>Virtual function table pointer.</summary>
    public uint VTablePointer { get; }

    /// <summary>Win32 thread identifier (<c>_m_dwThreadId</c>).</summary>
    public uint ThreadId { get; }

    /// <summary>Win32 thread handle (<c>_m_hThread</c>).</summary>
    public uint ThreadHandle { get; }

    /// <summary>Creates a <see cref="ZThread"/> with the specified fields.</summary>
    public ZThread(uint vTablePointer, uint threadId, uint threadHandle)
    {
        VTablePointer = vTablePointer;
        ThreadId = threadId;
        ThreadHandle = threadHandle;
    }

    /// <summary>Reads a <c>ZThread</c> from binary data at <paramref name="fileOffset"/>.</summary>
    /// <param name="image">Raw PE image bytes.</param>
    /// <param name="fileOffset">File offset of the struct start.</param>
    public static ZThread ReadFrom(ReadOnlySpan<byte> image, int fileOffset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZThreadLayout.VTableOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZThreadLayout.ThreadIdOffset)..]),
            BinaryPrimitives.ReadUInt32LittleEndian(image[(fileOffset + ZThreadLayout.ThreadHandleOffset)..])
        );
}
