using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Addresses of a writable native <c>StringPool</c> allocation and its cache payloads.
/// </summary>
public readonly record struct NativeStringPoolAllocation(
    uint ObjectAddress,
    uint NarrowCacheBase,
    uint NarrowCachePayload,
    uint WideCacheBase,
    uint WideCachePayload,
    int SlotCount
);

/// <summary>
/// Builds live-writable <c>StringPool</c> object layouts for substitution scenarios.
/// </summary>
public static class NativeStringPool
{
    /// <summary>
    /// Allocates an empty StringPool matching the original GMS v95 constructor invariant.
    /// </summary>
    public static NativeStringPoolAllocation AllocateV95(INativeAllocator allocator) =>
        AllocateEmpty(allocator, StringPoolLayout.GmsV95SlotCount);

    /// <summary>
    /// Allocates an empty GMS v95 StringPool and returns the object address.
    /// </summary>
    public static uint CreateV95(INativeAllocator allocator) => AllocateV95(allocator).ObjectAddress;

    /// <summary>
    /// Allocates an empty <c>StringPool</c> instance and returns all relevant addresses.
    /// </summary>
    /// <remarks>
    /// This overload accepts an arbitrary slot count for tests and synthetic layouts.
    /// For runtime-substitutable v95 objects, prefer <see cref="AllocateV95"/>.
    /// </remarks>
    public static NativeStringPoolAllocation AllocateEmpty(INativeAllocator allocator, int slotCount)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        if (slotCount < 0)
            throw new ArgumentOutOfRangeException(nameof(slotCount));

        ZArray<uint>.Allocation narrowCache = ZArray<uint>.Allocate(allocator, new uint[slotCount]);
        ZArray<uint>.Allocation wideCache = ZArray<uint>.Allocate(allocator, new uint[slotCount]);

        Span<byte> objectBytes = stackalloc byte[StringPoolLayout.TotalBytes];
        objectBytes.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(
            objectBytes[StringPoolLayout.NarrowCacheOffset..],
            narrowCache.PayloadAddress
        );
        BinaryPrimitives.WriteUInt32LittleEndian(
            objectBytes[StringPoolLayout.WideCacheOffset..],
            wideCache.PayloadAddress
        );
        // m_lock is already zeroed: _m_pTIB = null, _m_nRef = 0.

        uint objectAddress = allocator.Allocate(objectBytes.Length);
        if (!allocator.Write(objectAddress, objectBytes))
        {
            allocator.Free(objectAddress);
            throw new InvalidOperationException("Failed to write the StringPool object allocation.");
        }

        return new NativeStringPoolAllocation(
            objectAddress,
            narrowCache.BaseAddress,
            narrowCache.PayloadAddress,
            wideCache.BaseAddress,
            wideCache.PayloadAddress,
            slotCount
        );
    }

    /// <summary>
    /// Allocates an empty <c>StringPool</c> instance with zeroed narrow/wide caches and an unlocked lock.
    /// </summary>
    /// <returns>The x86 address of the allocated <c>StringPool</c> object.</returns>
    /// <remarks>
    /// For runtime-substitutable v95 objects, prefer <see cref="CreateV95"/>.
    /// </remarks>
    public static uint CreateEmpty(INativeAllocator allocator, int slotCount) =>
        AllocateEmpty(allocator, slotCount).ObjectAddress;

    /// <summary>Writes a narrow-cache slot to point at a native <c>ZXString&lt;char&gt;</c> object.</summary>
    public static void SetNarrowSlot(
        INativeAllocator allocator,
        NativeStringPoolAllocation pool,
        int index,
        uint zxStringAddress
    ) => WriteSlot(allocator, pool.NarrowCachePayload, pool.SlotCount, index, zxStringAddress);

    /// <summary>Writes a wide-cache slot to point at a native <c>ZXString&lt;unsigned short&gt;</c> object.</summary>
    public static void SetWideSlot(
        INativeAllocator allocator,
        NativeStringPoolAllocation pool,
        int index,
        uint zxStringAddress
    ) => WriteSlot(allocator, pool.WideCachePayload, pool.SlotCount, index, zxStringAddress);

    /// <summary>
    /// Releases a <c>StringPool</c> allocation and optionally destroys all cached string objects it owns.
    /// </summary>
    public static void Destroy(
        INativeAllocator allocator,
        NativeStringPoolAllocation pool,
        bool destroyNarrowStrings = false,
        bool destroyWideStrings = false
    )
    {
        ArgumentNullException.ThrowIfNull(allocator);

        HashSet<uint>? destroyedNarrowStrings = destroyNarrowStrings ? [] : null;
        HashSet<uint>? destroyedWideStrings = destroyWideStrings ? [] : null;

        if (destroyNarrowStrings)
            DestroySlots(allocator, pool.NarrowCachePayload, pool.SlotCount, ZXString.Destroy, destroyedNarrowStrings);

        if (destroyWideStrings)
        {
            DestroySlots(allocator, pool.WideCachePayload, pool.SlotCount, ZXStringWide.Destroy, destroyedWideStrings);
        }

        allocator.Free(pool.ObjectAddress);
        allocator.Free(pool.NarrowCacheBase);
        allocator.Free(pool.WideCacheBase);
    }

    private static void WriteSlot(INativeAllocator allocator, uint payloadAddress, int slotCount, int index, uint value)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, slotCount);

        Span<byte> bytes = stackalloc byte[TypeSizes.Pointer];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        uint slotAddress = checked(payloadAddress + (uint)(index * TypeSizes.Pointer));
        if (!allocator.Write(slotAddress, bytes))
            throw new InvalidOperationException($"Failed to write StringPool slot {index} at 0x{slotAddress:X8}.");
    }

    private static void DestroySlots(
        INativeAllocator allocator,
        uint payloadAddress,
        int slotCount,
        Action<INativeAllocator, uint> destroy,
        HashSet<uint>? destroyedAddresses
    )
    {
        Span<byte> pointerBytes = stackalloc byte[TypeSizes.Pointer];
        for (int index = 0; index < slotCount; index++)
        {
            uint slotAddress = checked(payloadAddress + (uint)(index * TypeSizes.Pointer));
            if (!allocator.Read(slotAddress, pointerBytes))
                throw new InvalidOperationException($"Failed to read StringPool slot {index} at 0x{slotAddress:X8}.");

            uint stringAddress = BinaryPrimitives.ReadUInt32LittleEndian(pointerBytes);
            if (stringAddress != 0 && (destroyedAddresses is null || destroyedAddresses.Add(stringAddress)))
                destroy(allocator, stringAddress);
        }
    }
}
