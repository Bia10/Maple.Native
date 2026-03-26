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
// REPLACE THIS with actual example calls to your library.
// This exact method body will appear in the README "## Example" section.
Native.Empty();
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
// REPLACE THIS with actual example calls to your library.
// This exact method body will appear in the README "## Example" section.
Native.Empty();
```

## Public API Reference

See [docs/PublicApi.md](docs/PublicApi.md) for the complete auto-generated public API reference.

> **Note**: `docs/PublicApi.md` is auto-updated by the `ReadMeTest_PublicApi` test on every `dotnet test` run. Do not edit it manually.
