# Maple.Native

![.NET](https://img.shields.io/badge/net10.0-5C2D91?logo=.NET&labelColor=gray)
![C#](https://img.shields.io/badge/C%23-14.0-239120?labelColor=gray)
[![Build Status](https://github.com/Bia10/Maple.Native/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/Bia10/Maple.Native/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/Bia10/Maple.Native/branch/main/graph/badge.svg)](https://codecov.io/gh/Bia10/Maple.Native)
[![Nuget](https://img.shields.io/nuget/v/Maple.Native?color=purple)](https://www.nuget.org/packages/Maple.Native/)
[![License](https://img.shields.io/github/license/Bia10/Maple.Native)](https://github.com/Bia10/Maple.Native/blob/main/LICENSE)

MapleStory GMS v95 native type layouts and runtime mutation primitives: ZArray, ZXString, ZFatalSection, StringPool lock helpers, and x86 allocator contracts. Foundation layer for maple-memory and maple-client-v95. Cross-platform, trimmable and AOT/NativeAOT compatible.

⭐ Please star this project if you like it. ⭐

[Example](#example) | [Example Catalogue](#example-catalogue) | [Public API](docs/PublicApi.md)

## Example

```csharp
// Allocate a synthetic x86 address space for testing native types out-of-process.
using var allocator = new InProcessAllocator();

// Allocate a ZXString (ANSI ref-counted string) and read it back.
var strAddr = ZXString.Create(allocator, "MapleStory");
var alloc = ZXString.Allocate(allocator, "MapleStory");
ZXString.Destroy(allocator, strAddr);
ZXString.Destroy(allocator, alloc.ObjectAddress);

// Allocate a wide (UTF-16) string.
var wideAddr = ZXStringWide.Create(allocator, "메이플스토리");
ZXStringWide.Destroy(allocator, wideAddr);

// Spin-lock round-trip: acquire the StringPool lock, then release.
var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 4);
using (NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100))
{
    var locked = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
    _ = locked.TibPointer; // non-zero while held
}
NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
```

For more examples see [Example Catalogue](#example-catalogue).

## Benchmarks

Benchmarks.

### Detailed Benchmarks

#### Comparison Benchmarks

##### TestBench Benchmark Results

###### Results will be populated here after running `dotnet Build.cs comparison-bench` then `dotnet test`

## Example Catalogue

The following examples are available in [ReadMeTest.cs](src/Maple.Native.DocTest/ReadMeTest.cs).

### Example - Empty

```csharp
// Allocate a synthetic x86 address space for testing native types out-of-process.
using var allocator = new InProcessAllocator();

// Allocate a ZXString (ANSI ref-counted string) and read it back.
var strAddr = ZXString.Create(allocator, "MapleStory");
var alloc = ZXString.Allocate(allocator, "MapleStory");
ZXString.Destroy(allocator, strAddr);
ZXString.Destroy(allocator, alloc.ObjectAddress);

// Allocate a wide (UTF-16) string.
var wideAddr = ZXStringWide.Create(allocator, "메이플스토리");
ZXStringWide.Destroy(allocator, wideAddr);

// Spin-lock round-trip: acquire the StringPool lock, then release.
var pool = NativeStringPool.AllocateEmpty(allocator, slotCount: 4);
using (NativeStringPoolLock.Acquire(allocator, pool.ObjectAddress, maxSpinCount: 100))
{
    var locked = NativeStringPoolLock.Read(allocator, pool.ObjectAddress);
    _ = locked.TibPointer; // non-zero while held
}
NativeStringPool.Destroy(allocator, pool, destroyNarrowStrings: false, destroyWideStrings: false);
```

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete auto-generated public API reference.

> **Note**: `docs/PublicApi.md` is auto-updated by the `ReadMeTest_PublicApi` test on every `dotnet test` run. Do not edit it manually.
