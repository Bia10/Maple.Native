using System.Buffers.Binary;
using System.Threading;

namespace Maple.Native;

/// <summary>
/// Test and tooling allocator that exposes a synthetic x86 address space within the current process.
/// </summary>
/// <remarks>
/// <para>
/// This allocator is designed for building faithful Maple client layouts on any host architecture.
/// It returns synthetic 32-bit addresses and stores the backing bytes in managed memory,
/// so callers can exercise native layout writers without requiring a real x86 process.
/// </para>
/// <para>
/// A future remote allocator can implement <see cref="INativeAllocator"/> against real
/// process memory and reuse the same create APIs unchanged.
/// </para>
/// </remarks>
public sealed class InProcessAllocator : INativeRuntimeAllocator, IDisposable
{
    private const uint DefaultBaseAddress = 0x1000_0000u;
    private const uint ThreadTokenBase = 0x7000_0000u;
    private readonly object _sync = new();
    private readonly List<Allocation> _allocations = [];
    private readonly ThreadLocal<uint> _currentThreadTeb = new(CreateThreadToken);
    private static int s_nextThreadToken;
    private uint _nextAddress = DefaultBaseAddress;
    private bool _disposed;

    /// <inheritdoc/>
    public uint CurrentThreadTeb
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _currentThreadTeb.Value;
        }
    }

    /// <inheritdoc/>
    public uint Allocate(int size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Allocation size must be positive.");

        lock (_sync)
        {
            uint alignedSize = Align4((uint)size);
            uint address = _nextAddress;
            uint next = checked(address + alignedSize);
            _allocations.Add(new Allocation(address, new byte[size]));
            _nextAddress = next;
            return address;
        }
    }

    /// <inheritdoc/>
    public bool Write(uint address, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            if (!TryLocate(address, data.Length, out Allocation allocation, out int offset))
                return false;

            data.CopyTo(allocation.Buffer.AsSpan(offset, data.Length));
            return true;
        }
    }

    /// <inheritdoc/>
    public void Free(uint address)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            int index = -1;
            for (int i = 0; i < _allocations.Count; i++)
            {
                if (_allocations[i].BaseAddress == address)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(address), $"Unknown allocation base 0x{address:X8}.");

            _allocations.RemoveAt(index);
        }
    }

    /// <inheritdoc/>
    public bool Read(uint address, Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            if (!TryLocate(address, destination.Length, out Allocation allocation, out int offset))
                return false;

            allocation.Buffer.AsSpan(offset, destination.Length).CopyTo(destination);
            return true;
        }
    }

    /// <inheritdoc/>
    public bool CompareExchangeUInt32(uint address, uint expected, uint desired, out uint observed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            if (!TryLocate(address, TypeSizes.Pointer, out Allocation allocation, out int offset))
            {
                observed = 0;
                return false;
            }

            Span<byte> slice = allocation.Buffer.AsSpan(offset, TypeSizes.Pointer);
            observed = BinaryPrimitives.ReadUInt32LittleEndian(slice);
            if (observed != expected)
                return false;

            BinaryPrimitives.WriteUInt32LittleEndian(slice, desired);
            return true;
        }
    }

    /// <inheritdoc/>
    public void YieldThread() => Thread.Yield();

    /// <summary>Reads a copied byte array from the synthetic address space.</summary>
    public byte[] ReadBytes(uint address, int count)
    {
        var result = new byte[count];
        if (!Read(address, result))
            throw new ArgumentOutOfRangeException(
                nameof(address),
                $"Address range 0x{address:X8}+{count} is not allocated."
            );

        return result;
    }

    /// <summary>Reads a 32-bit little-endian value from the synthetic address space.</summary>
    public uint ReadUInt32(uint address)
    {
        Span<byte> buffer = stackalloc byte[TypeSizes.Pointer];
        if (!Read(address, buffer))
            throw new ArgumentOutOfRangeException(nameof(address), $"Address 0x{address:X8} is not allocated.");

        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_sync)
        {
            _allocations.Clear();
            _currentThreadTeb.Dispose();
            _disposed = true;
        }
    }

    private bool TryLocate(uint address, int size, out Allocation allocation, out int offset)
    {
        foreach (Allocation candidate in _allocations)
        {
            uint start = candidate.BaseAddress;
            uint end = checked(start + (uint)candidate.Buffer.Length);
            uint requestedEnd = checked(address + (uint)size);
            if (address >= start && requestedEnd <= end)
            {
                allocation = candidate;
                offset = (int)(address - start);
                return true;
            }
        }

        allocation = null!;
        offset = 0;
        return false;
    }

    private static uint Align4(uint size) => (size + 3u) & ~3u;

    private static uint CreateThreadToken() =>
        checked(ThreadTokenBase + ((uint)Interlocked.Increment(ref s_nextThreadToken) * 0x1000u));

    private sealed class Allocation(uint baseAddress, byte[] buffer)
    {
        public uint BaseAddress { get; } = baseAddress;

        public byte[] Buffer { get; } = buffer;
    }
}
