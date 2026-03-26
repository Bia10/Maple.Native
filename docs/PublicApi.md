# Public API Reference

## Public API Reference

```csharp
[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Bia10/Maple.Native/")]
[assembly: System.Resources.NeutralResourcesLanguage("en")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Native.Benchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Native.ComparisonBenchmarks")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Native.DocTest")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Maple.Native.Test")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace Maple.Native
{
    public readonly struct CInPacket : Maple.Native.INativeSized
    {
        public CInPacket(bool isLoopback, int state, uint recvBuffPointer, ushort length, ushort rawSeq, ushort dataLen, uint readOffset) { }
        public ushort DataLen { get; }
        public bool IsLoopback { get; }
        public ushort Length { get; }
        public ushort RawSeq { get; }
        public uint ReadOffset { get; }
        public uint RecvBuffPointer { get; }
        public int State { get; }
        public static int NativeSize { get; }
        public static Maple.Native.CInPacket ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct CInPacketLayout
    {
        public const int DataLenOffset = 16;
        public const int LengthOffset = 12;
        public const int LoopbackOffset = 0;
        public const int RawSeqOffset = 14;
        public const int ReadOffsetOffset = 20;
        public const int RecvBuffOffset = 8;
        public const int StateOffset = 4;
        public const int TotalBytes = 24;
    }
    public readonly struct COutPacket : Maple.Native.INativeSized
    {
        public COutPacket(bool isLoopback, uint sendBuffPointer, uint writeOffset, bool isEncryptedByShanda) { }
        public bool IsEncryptedByShanda { get; }
        public bool IsLoopback { get; }
        public uint SendBuffPointer { get; }
        public uint WriteOffset { get; }
        public static int NativeSize { get; }
        public static Maple.Native.COutPacket ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct COutPacketLayout
    {
        public const int LoopbackOffset = 0;
        public const int SendBuffOffset = 4;
        public const int ShandaFlagOffset = 12;
        public const int TotalBytes = 16;
        public const int WriteOffsetOffset = 8;
    }
    public interface INativeAllocator
    {
        uint Allocate(int size);
        void Free(uint address);
        bool Read(uint address, System.Span<byte> destination);
        bool Write(uint address, System.ReadOnlySpan<byte> data);
    }
    public interface INativeRuntimeAllocator : Maple.Native.INativeAllocator
    {
        uint CurrentThreadTeb { get; }
        bool CompareExchangeUInt32(uint address, uint expected, uint desired, out uint observed);
        void YieldThread();
    }
    public interface INativeSized
    {
        int NativeSize { get; }
    }
    public sealed class InProcessAllocator : Maple.Native.INativeAllocator, Maple.Native.INativeRuntimeAllocator, System.IDisposable
    {
        public InProcessAllocator() { }
        public uint CurrentThreadTeb { get; }
        public uint Allocate(int size) { }
        public bool CompareExchangeUInt32(uint address, uint expected, uint desired, out uint observed) { }
        public void Dispose() { }
        public void Free(uint address) { }
        public bool Read(uint address, System.Span<byte> destination) { }
        public byte[] ReadBytes(uint address, int count) { }
        public uint ReadUInt32(uint address) { }
        public bool Write(uint address, System.ReadOnlySpan<byte> data) { }
        public void YieldThread() { }
    }
    public static class Native
    {
        public static void Empty() { }
    }
    public static class NativeCast
    {
        public static T? As<T>(System.ReadOnlySpan<byte> image, int offset, System.Func<System.ReadOnlySpan<byte>, int, T> reader)
            where T :  struct { }
        public static bool Is<T>(System.ReadOnlySpan<byte> image, int offset)
            where T : Maple.Native.INativeSized { }
        public static string NameOf<T>() { }
        public static T Reinterpret<T>(nint ptr)
            where T :  unmanaged { }
        public static T Reinterpret<T>(System.ReadOnlySpan<byte> image, int offset)
            where T :  unmanaged { }
        public static bool SafeRead<T>(nint ptr, out T result)
            where T :  unmanaged { }
        public static int SizeOf<T>()
            where T : Maple.Native.INativeSized { }
        public static bool TryCast<T>(System.ReadOnlySpan<byte> image, int offset, System.Func<System.ReadOnlySpan<byte>, int, T> reader, out T result) { }
    }
    public sealed class NativeImageView
    {
        public NativeImageView(System.ReadOnlyMemory<byte> image, uint imageBase) { }
        public uint ImageBase { get; }
        public T? As<T>(Maple.Native.NativePtr<T> ptr, System.Func<System.ReadOnlySpan<byte>, int, T> reader)
            where T :  struct { }
        public T? As<T>(uint address, System.Func<System.ReadOnlySpan<byte>, int, T> reader)
            where T :  struct { }
        public T Cast<T>(Maple.Native.NativePtr<T> ptr, System.Func<System.ReadOnlySpan<byte>, int, T> reader) { }
        public T Cast<T>(uint address, System.Func<System.ReadOnlySpan<byte>, int, T> reader) { }
        public bool Contains(uint address, int sizeHint = 1) { }
        public int FileOffset(uint address) { }
        public bool Is<T>(Maple.Native.NativePtr<T> ptr)
            where T : Maple.Native.INativeSized { }
        public bool Is<T>(uint address)
            where T : Maple.Native.INativeSized { }
        public T[] ReadZArrayOfPtrs<T>(uint payloadRva, System.Func<System.ReadOnlySpan<byte>, int, T> elementReader) { }
        public T[] ReadZArrayOfPtrs<T>(uint payloadRva, int count, System.Func<System.ReadOnlySpan<byte>, int, T> elementReader) { }
        public Maple.Native.ZXString[] ReadZArrayOfZXString(uint payloadRva) { }
        public Maple.Native.ZXString[] ReadZArrayOfZXString(uint payloadRva, int count) { }
        public Maple.Native.ZXStringWide[] ReadZArrayOfZXStringWide(uint payloadRva) { }
        public Maple.Native.ZXStringWide[] ReadZArrayOfZXStringWide(uint payloadRva, int count) { }
        public bool TryCast<T>(Maple.Native.NativePtr<T> ptr, System.Func<System.ReadOnlySpan<byte>, int, T> reader, out T result) { }
        public bool TryCast<T>(uint address, System.Func<System.ReadOnlySpan<byte>, int, T> reader, out T result) { }
    }
    public readonly struct NativePtr<T>
    {
        public NativePtr(uint address) { }
        public uint Address { get; }
        public bool IsNull { get; }
        public static Maple.Native.NativePtr<T> Null { get; }
        public Maple.Native.NativePtr<T> Add(int byteOffset) { }
        public bool Equals(Maple.Native.NativePtr<T> other) { }
        public override bool Equals(object? obj) { }
        public override int GetHashCode() { }
        public bool IsInBounds(Maple.Native.NativeImageView view) { }
        public T Read(Maple.Native.NativeImageView view, System.Func<System.ReadOnlySpan<byte>, int, T> reader) { }
        public T Reinterpret(System.Func<System.IntPtr, T> converter) { }
        public override string ToString() { }
        public bool TryRead(Maple.Native.NativeImageView view, System.Func<System.ReadOnlySpan<byte>, int, T> reader, out T result) { }
        public static uint op_Explicit(Maple.Native.NativePtr<T> ptr) { }
        public static Maple.Native.NativePtr<T> op_Implicit(uint address) { }
        public static bool operator !=(Maple.Native.NativePtr<T> left, Maple.Native.NativePtr<T> right) { }
        public static bool operator ==(Maple.Native.NativePtr<T> left, Maple.Native.NativePtr<T> right) { }
    }
    public static class NativeStringPool
    {
        public static Maple.Native.NativeStringPoolAllocation AllocateEmpty(Maple.Native.INativeAllocator allocator, int slotCount) { }
        public static Maple.Native.NativeStringPoolAllocation AllocateV95(Maple.Native.INativeAllocator allocator) { }
        public static uint CreateEmpty(Maple.Native.INativeAllocator allocator, int slotCount) { }
        public static uint CreateV95(Maple.Native.INativeAllocator allocator) { }
        public static void Destroy(Maple.Native.INativeAllocator allocator, Maple.Native.NativeStringPoolAllocation pool, bool destroyNarrowStrings = false, bool destroyWideStrings = false) { }
        public static void SetNarrowSlot(Maple.Native.INativeAllocator allocator, Maple.Native.NativeStringPoolAllocation pool, int index, uint zxStringAddress) { }
        public static void SetWideSlot(Maple.Native.INativeAllocator allocator, Maple.Native.NativeStringPoolAllocation pool, int index, uint zxStringAddress) { }
    }
    public readonly struct NativeStringPoolAllocation : System.IEquatable<Maple.Native.NativeStringPoolAllocation>
    {
        public NativeStringPoolAllocation(uint ObjectAddress, uint NarrowCacheBase, uint NarrowCachePayload, uint WideCacheBase, uint WideCachePayload, int SlotCount) { }
        public uint NarrowCacheBase { get; init; }
        public uint NarrowCachePayload { get; init; }
        public uint ObjectAddress { get; init; }
        public int SlotCount { get; init; }
        public uint WideCacheBase { get; init; }
        public uint WideCachePayload { get; init; }
    }
    public static class NativeStringPoolLock
    {
        public static Maple.Native.NativeStringPoolLockScope Acquire(Maple.Native.INativeRuntimeAllocator allocator, uint stringPoolAddress, int maxSpinCount = 4096) { }
        public static Maple.Native.ZFatalSection Read(Maple.Native.INativeAllocator allocator, uint stringPoolAddress) { }
    }
    public sealed class NativeStringPoolLockScope : System.IDisposable
    {
        public void Dispose() { }
    }
    public readonly ref struct StringPoolKeyLayout
    {
        public const int KeyArrayOffset = 0;
        public const int TotalBytes = 4;
    }
    public readonly ref struct StringPoolLayout
    {
        public const int GmsV95SlotCount = 6883;
        public const int LockOffset = 8;
        public const int NarrowCacheOffset = 0;
        public const int TotalBytes = 16;
        public const int WideCacheOffset = 4;
    }
    public static class TypeSizes
    {
        public const int Int32 = 4;
        public const int Pointer = 4;
        public const int UInt32 = 4;
    }
    public readonly ref struct WsaBufLayout
    {
        public const int BufOffset = 4;
        public const int LenOffset = 0;
        public const int TotalBytes = 8;
    }
    public readonly ref struct ZAllocAnonSelectorLayout
    {
        public const int TotalBytes = 0;
    }
    public readonly ref struct ZAllocBaseLayout
    {
        public const int TotalBytes = 0;
    }
    public readonly ref struct ZAllocExLayout
    {
        public const int BlockHeadCount = 4;
        public const int BlockHeadOffset = 28;
        public const int BuffCount = 4;
        public const int BuffOffset = 12;
        public const int GapOffset = 0;
        public const int LockOffset = 4;
        public const int TotalBytes = 44;
    }
    public readonly ref struct ZAllocHelperLayout
    {
        public const int TotalBytes = 0;
    }
    public readonly ref struct ZAllocStrSelectorLayout
    {
        public const int TotalBytes = 0;
    }
    public static class ZArray
    {
        public static byte[] ReadByteElements(System.ReadOnlySpan<byte> image, int payloadFileOffset, int count) { }
        public static int ReadCount(System.ReadOnlySpan<byte> image, int payloadFileOffset) { }
        public static uint[] ReadPointerElements(System.ReadOnlySpan<byte> image, int payloadFileOffset, int count) { }
    }
    public readonly ref struct ZArrayLayout
    {
        public const int CountOffset = 0;
        public const int HeaderBytes = 4;
        public const int PayloadOffset = 4;
        public ZArrayLayout(int elementCount) { }
        public int ElementCount { get; }
        public int TotalBytes(int elementSize) { }
    }
    public static class ZArray<T>
        where T :  unmanaged
    {
        public static Maple.Native.ZArray<T>.Allocation Allocate(Maple.Native.INativeAllocator allocator, System.ReadOnlySpan<T> elements) { }
        public static uint Create(Maple.Native.INativeAllocator allocator, System.ReadOnlySpan<T> elements) { }
        public static void Destroy(Maple.Native.INativeAllocator allocator, uint payloadAddress) { }
        public readonly struct Allocation : System.IEquatable<Maple.Native.ZArray<T>.Allocation>
        {
            public Allocation(uint BaseAddress, uint PayloadAddress, int ElementCount) { }
            public uint BaseAddress { get; init; }
            public int ElementCount { get; init; }
            public uint PayloadAddress { get; init; }
        }
    }
    public readonly struct ZFatalSection : Maple.Native.INativeSized
    {
        public ZFatalSection(uint tibPointer, int refCount) { }
        public int RefCount { get; }
        public uint TibPointer { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZFatalSection Unlocked { get; }
    }
    public readonly ref struct ZFatalSectionLayout
    {
        public const int RefCountOffset = 4;
        public const int TibPointerOffset = 0;
        public const int TotalBytes = 8;
    }
    public readonly struct ZInetAddr : Maple.Native.INativeSized
    {
        public ZInetAddr(short family, ushort portNetworkOrder, uint address) { }
        public uint Address { get; }
        public short Family { get; }
        public ushort Port { get; }
        public ushort PortNetworkOrder { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZInetAddr ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZInetAddrLayout
    {
        public const int AddrOffset = 4;
        public const int FamilyOffset = 0;
        public const int PortOffset = 2;
        public const int TotalBytes = 16;
        public const int ZeroOffset = 8;
    }
    public static class ZList
    {
        public static uint ReadCount(System.ReadOnlySpan<byte> image, int fileOffset) { }
        public static uint ReadHead(System.ReadOnlySpan<byte> image, int fileOffset) { }
        public static uint ReadTail(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZListLayout
    {
        public const int CountOffset = 8;
        public const int GapOffset = 4;
        public const int HeadOffset = 12;
        public const int TailOffset = 16;
        public const int TotalBytes = 20;
        public const int VTableOffset = 0;
    }
    public static class ZMap
    {
        public static uint ReadBucketHead(System.ReadOnlySpan<byte> image, int tableFileOffset, int bucketIndex) { }
        public static uint ReadPairNext(System.ReadOnlySpan<byte> image, int nodeFileOffset) { }
    }
    public readonly struct ZMapHeader
    {
        public ZMapHeader(uint tableVa, uint tableSize, uint count, uint autoGrowEvery128, uint autoGrowLimit) { }
        public uint AutoGrowEvery128 { get; }
        public uint AutoGrowLimit { get; }
        public uint Count { get; }
        public uint TableSize { get; }
        public uint TableVa { get; }
        public static Maple.Native.ZMapHeader ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZMapLayout
    {
        public const int AutoGrowEvery128Offset = 16;
        public const int AutoGrowLimitOffset = 20;
        public const int CountOffset = 12;
        public const int TableOffset = 4;
        public const int TableSizeOffset = 8;
        public const int TotalBytes = 24;
        public const int VTableOffset = 0;
    }
    public readonly ref struct ZMapPairLayout
    {
        public const int KeyOffset = 8;
        public const int NextOffset = 4;
        public const int VTableOffset = 0;
        public ZMapPairLayout(int keyBytes, int valueBytes) { }
        public int KeyBytes { get; }
        public int TotalBytes { get; }
        public int ValueBytes { get; }
        public int ValueOffset { get; }
        public static Maple.Native.ZMapPairLayout IntInt { get; }
        public static Maple.Native.ZMapPairLayout XStringZPair { get; }
    }
    public readonly struct ZPair : Maple.Native.INativeSized
    {
        public ZPair(int first, int second) { }
        public int First { get; }
        public int Second { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZPair ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZPairLayout
    {
        public const int FirstOffset = 0;
        public const int SecondOffset = 4;
        public const int TotalBytes = 8;
    }
    public readonly ref struct ZRecyclableLayout
    {
        public const int TotalBytes = 4;
        public const int VTableOffset = 0;
    }
    public readonly ref struct ZRecyclableStaticLayout
    {
        public const int HeadOffset = 0;
        public const int TotalBytes = 4;
    }
    public static class ZRef
    {
        public static uint ReadPointer(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly struct ZRefCounted : Maple.Native.INativeSized
    {
        public ZRefCounted(uint vTablePointer, int refCount, uint prevPointer) { }
        public uint PrevPointer { get; }
        public int RefCount { get; }
        public uint VTablePointer { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZRefCounted ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly struct ZRefCountedDummyHeader
    {
        public ZRefCountedDummyHeader(uint vTablePointer, uint nextPointer, uint prevPointer, uint recyclableVTablePointer) { }
        public uint NextPointer { get; }
        public uint PrevPointer { get; }
        public uint RecyclableVTablePointer { get; }
        public uint VTablePointer { get; }
        public static Maple.Native.ZRefCountedDummyHeader ReadFrom(System.ReadOnlySpan<byte> image, int nodeBaseFileOffset) { }
    }
    public readonly ref struct ZRefCountedDummyLayout
    {
        public const int DataOffset = 16;
        public const int HeaderBytes = 16;
        public const int PrevPointerOffset = 8;
        public const int RecyclableVTableOffset = 12;
        public const int RefCountOrNextOffset = 4;
        public const int VTableOffset = 0;
    }
    public readonly ref struct ZRefCountedLayout
    {
        public const int PrevOffset = 8;
        public const int RefCountOffset = 4;
        public const int TotalBytes = 12;
        public const int VTableOffset = 0;
    }
    public readonly ref struct ZRefLayout
    {
        public const int GapBytes = 4;
        public const int PointerOffset = 4;
        public const int TotalBytes = 8;
    }
    public readonly struct ZSocketBase : Maple.Native.INativeSized
    {
        public ZSocketBase(uint socketHandle) { }
        public uint SocketHandle { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZSocketBase ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZSocketBaseLayout
    {
        public const int SocketHandleOffset = 0;
        public const int TotalBytes = 4;
    }
    public readonly struct ZSocketBuffer : Maple.Native.INativeSized
    {
        public ZSocketBuffer(uint vTablePointer, int refCount, uint prevPointer, uint wsaLen, uint wsaBuf, uint parentPointer) { }
        public uint ParentPointer { get; }
        public uint PrevPointer { get; }
        public int RefCount { get; }
        public uint VTablePointer { get; }
        public uint WsaBuf { get; }
        public uint WsaLen { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZSocketBuffer ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZSocketBufferLayout
    {
        public const int ParentPointerOffset = 24;
        public const int PrevOffset = 8;
        public const int RefCountOffset = 4;
        public const int TotalBytes = 28;
        public const int VTableOffset = 0;
        public const int WsaBufOffset = 16;
        public const int WsaLenOffset = 12;
    }
    public readonly ref struct ZSyncAutoUnlockLayout
    {
        public const int LockPointerOffset = 0;
        public const int TotalBytes = 4;
    }
    public readonly struct ZThread : Maple.Native.INativeSized
    {
        public ZThread(uint vTablePointer, uint threadId, uint threadHandle) { }
        public uint ThreadHandle { get; }
        public uint ThreadId { get; }
        public uint VTablePointer { get; }
        public static int NativeSize { get; }
        public static Maple.Native.ZThread ReadFrom(System.ReadOnlySpan<byte> image, int fileOffset) { }
    }
    public readonly ref struct ZThreadLayout
    {
        public const int ThreadHandleOffset = 8;
        public const int ThreadIdOffset = 4;
        public const int TotalBytes = 12;
        public const int VTableOffset = 0;
    }
    public readonly struct ZXString
    {
        public ZXString(string value, int refCount = 1, int capacity = 0, int byteLength = 0) { }
        public int ByteLength { get; }
        public int Capacity { get; }
        public int RefCount { get; }
        public string Value { get; }
        public override string ToString() { }
        public static Maple.Native.ZXString.Allocation Allocate(Maple.Native.INativeAllocator allocator, System.ReadOnlySpan<byte> payload, int refCount = 1, int capacity = 0) { }
        public static Maple.Native.ZXString.Allocation Allocate(Maple.Native.INativeAllocator allocator, string value, int refCount = 1, int capacity = 0) { }
        public static uint Create(Maple.Native.INativeAllocator allocator, System.ReadOnlySpan<byte> payload, int refCount = 1, int capacity = 0) { }
        public static uint Create(Maple.Native.INativeAllocator allocator, string value, int refCount = 1, int capacity = 0) { }
        public static void Destroy(Maple.Native.INativeAllocator allocator, uint objectAddress) { }
        public static Maple.Native.ZXString ReadFrom(System.ReadOnlySpan<byte> image, int payloadFileOffset) { }
        public static string op_Implicit(Maple.Native.ZXString s) { }
        public readonly struct Allocation : System.IEquatable<Maple.Native.ZXString.Allocation>
        {
            public Allocation(uint ObjectAddress, uint DataAddress, uint PayloadAddress, int ByteLength, int Capacity) { }
            public int ByteLength { get; init; }
            public int Capacity { get; init; }
            public uint DataAddress { get; init; }
            public uint ObjectAddress { get; init; }
            public uint PayloadAddress { get; init; }
        }
    }
    public readonly ref struct ZXStringDataLayout
    {
        public const int ByteLengthOffset = 8;
        public const int CapacityOffset = 4;
        public const int HeaderBytes = 12;
        public const int NullTerminatorBytes = 1;
        public const int PayloadOffset = 12;
        public const int RefCountOffset = 0;
        public ZXStringDataLayout(int payloadBytes) { }
        public int TotalBytes { get; }
    }
    public readonly ref struct ZXStringLayout
    {
        public const int StringPointerOffset = 0;
        public const int TotalBytes = 4;
    }
    public readonly struct ZXStringWide
    {
        public const int NullTerminatorBytes = 2;
        public ZXStringWide(string value, int refCount = 1, int capacity = 0, int byteLength = 0) { }
        public int ByteLength { get; }
        public int Capacity { get; }
        public int CharCount { get; }
        public int RefCount { get; }
        public string Value { get; }
        public override string ToString() { }
        public static Maple.Native.ZXStringWide.Allocation Allocate(Maple.Native.INativeAllocator allocator, string value, int refCount = 1, int capacity = 0) { }
        public static uint Create(Maple.Native.INativeAllocator allocator, string value, int refCount = 1, int capacity = 0) { }
        public static void Destroy(Maple.Native.INativeAllocator allocator, uint objectAddress) { }
        public static Maple.Native.ZXStringWide ReadFrom(System.ReadOnlySpan<byte> image, int payloadFileOffset) { }
        public static string op_Implicit(Maple.Native.ZXStringWide s) { }
        public readonly struct Allocation : System.IEquatable<Maple.Native.ZXStringWide.Allocation>
        {
            public Allocation(uint ObjectAddress, uint DataAddress, uint PayloadAddress, int ByteLength, int Capacity) { }
            public int ByteLength { get; init; }
            public int Capacity { get; init; }
            public uint DataAddress { get; init; }
            public uint ObjectAddress { get; init; }
            public uint PayloadAddress { get; init; }
        }
    }
}
```
