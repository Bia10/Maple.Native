using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Disposable hold on a live <c>StringPool::m_lock</c> acquisition.
/// </summary>
public sealed class NativeStringPoolLockScope : IDisposable
{
    private readonly INativeRuntimeAllocator _allocator;
    private readonly uint _lockAddress;
    private readonly uint _ownerTeb;
    private bool _disposed;

    internal NativeStringPoolLockScope(INativeRuntimeAllocator allocator, uint lockAddress, uint ownerTeb)
    {
        _allocator = allocator;
        _lockAddress = lockAddress;
        _ownerTeb = ownerTeb;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        NativeStringPoolLock.Release(_allocator, _lockAddress, _ownerTeb);
        _disposed = true;
    }
}

/// <summary>
/// Runtime helpers for acquiring and releasing <c>StringPool::m_lock</c>
/// using Maple's native <c>ZFatalSection</c> semantics.
/// </summary>
public static class NativeStringPoolLock
{
    private const int DefaultMaxSpinCount = 4096;

    /// <summary>
    /// Acquires <c>StringPool::m_lock</c> for the object at <paramref name="stringPoolAddress"/>.
    /// </summary>
    public static NativeStringPoolLockScope Acquire(
        INativeRuntimeAllocator allocator,
        uint stringPoolAddress,
        int maxSpinCount = DefaultMaxSpinCount
    )
    {
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSpinCount);

        uint ownerTeb = allocator.CurrentThreadTeb;
        if (ownerTeb == 0)
            throw new InvalidOperationException("CurrentThreadTeb must be non-zero for runtime lock acquisition.");

        uint lockAddress = checked(stringPoolAddress + (uint)StringPoolLayout.LockOffset);
        AcquireCore(allocator, lockAddress, ownerTeb, maxSpinCount);
        return new NativeStringPoolLockScope(allocator, lockAddress, ownerTeb);
    }

    /// <summary>
    /// Reads the current <c>StringPool::m_lock</c> state.
    /// </summary>
    public static ZFatalSection Read(INativeAllocator allocator, uint stringPoolAddress)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        uint lockAddress = checked(stringPoolAddress + (uint)StringPoolLayout.LockOffset);
        return ReadByAddress(allocator, lockAddress);
    }

    internal static void Release(INativeRuntimeAllocator allocator, uint lockAddress, uint ownerTeb)
    {
        ZFatalSection state = ReadByAddress(allocator, lockAddress);
        if (state.TibPointer != ownerTeb || state.RefCount <= 0)
        {
            throw new InvalidOperationException(
                $"Cannot release StringPool lock at 0x{lockAddress:X8}; it is owned by 0x{state.TibPointer:X8} with refcount {state.RefCount}."
            );
        }

        if (state.RefCount == 1)
        {
            WriteInt32(allocator, checked(lockAddress + (uint)ZFatalSectionLayout.RefCountOffset), 0);
            if (!allocator.CompareExchangeUInt32(lockAddress, ownerTeb, 0, out uint observed) || observed != ownerTeb)
            {
                throw new InvalidOperationException(
                    $"Failed to release StringPool lock at 0x{lockAddress:X8}; expected owner 0x{ownerTeb:X8}, observed 0x{observed:X8}."
                );
            }

            return;
        }

        WriteInt32(allocator, checked(lockAddress + (uint)ZFatalSectionLayout.RefCountOffset), state.RefCount - 1);
    }

    private static void AcquireCore(
        INativeRuntimeAllocator allocator,
        uint lockAddress,
        uint ownerTeb,
        int maxSpinCount
    )
    {
        for (int spin = 0; spin < maxSpinCount; spin++)
        {
            ZFatalSection state = ReadByAddress(allocator, lockAddress);
            if (state.TibPointer == ownerTeb)
            {
                WriteInt32(
                    allocator,
                    checked(lockAddress + (uint)ZFatalSectionLayout.RefCountOffset),
                    state.RefCount + 1
                );
                return;
            }

            if (state.TibPointer == 0)
            {
                if (!allocator.CompareExchangeUInt32(lockAddress, 0, ownerTeb, out uint observed))
                    throw new InvalidOperationException(
                        $"Failed to atomically acquire StringPool lock at 0x{lockAddress:X8}."
                    );

                if (observed == 0)
                {
                    WriteInt32(allocator, checked(lockAddress + (uint)ZFatalSectionLayout.RefCountOffset), 1);
                    return;
                }

                if (observed == ownerTeb)
                {
                    ZFatalSection reentrantState = ReadByAddress(allocator, lockAddress);
                    WriteInt32(
                        allocator,
                        checked(lockAddress + (uint)ZFatalSectionLayout.RefCountOffset),
                        reentrantState.RefCount + 1
                    );
                    return;
                }
            }

            allocator.YieldThread();
        }

        throw new TimeoutException(
            $"Timed out acquiring StringPool lock at 0x{lockAddress:X8} after {maxSpinCount} spins."
        );
    }

    private static ZFatalSection ReadByAddress(INativeAllocator allocator, uint lockAddress)
    {
        Span<byte> lockBytes = stackalloc byte[ZFatalSectionLayout.TotalBytes];
        if (!allocator.Read(lockAddress, lockBytes))
            throw new InvalidOperationException($"Failed to read StringPool lock at 0x{lockAddress:X8}.");

        return new ZFatalSection(
            BinaryPrimitives.ReadUInt32LittleEndian(lockBytes[ZFatalSectionLayout.TibPointerOffset..]),
            BinaryPrimitives.ReadInt32LittleEndian(lockBytes[ZFatalSectionLayout.RefCountOffset..])
        );
    }

    private static void WriteInt32(INativeAllocator allocator, uint address, int value)
    {
        Span<byte> bytes = stackalloc byte[TypeSizes.Int32];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        if (!allocator.Write(address, bytes))
            throw new InvalidOperationException($"Failed to write Int32 at 0x{address:X8}.");
    }
}
