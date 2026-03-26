namespace Maple.Native;

/// <summary>
/// Allocates and writes native x86 memory blocks for Maple client structs.
/// </summary>
/// <remarks>
/// Addresses are expressed as 32-bit virtual addresses because the GMS v95 client is x86.
/// Implementations may back these addresses with real remote-process memory or with a
/// synthetic address space for tests and in-process tooling.
/// </remarks>
public interface INativeAllocator
{
    /// <summary>Allocates a writable native block and returns its x86 base address.</summary>
    uint Allocate(int size);

    /// <summary>Reads bytes from <paramref name="address"/> into <paramref name="destination"/>.</summary>
    bool Read(uint address, Span<byte> destination);

    /// <summary>Writes <paramref name="data"/> to <paramref name="address"/>.</summary>
    bool Write(uint address, ReadOnlySpan<byte> data);

    /// <summary>Releases the block whose base address is <paramref name="address"/>.</summary>
    void Free(uint address);
}
