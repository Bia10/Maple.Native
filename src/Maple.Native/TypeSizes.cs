namespace Maple.Native;

/// <summary>
/// Platform constants for the GMS v95 x86 client.
/// </summary>
public static class TypeSizes
{
    /// <summary>x86 <c>int32</c> size — 4 bytes.</summary>
    public const int Int32 = sizeof(int);

    /// <summary>x86 <c>uint32</c> size — 4 bytes.</summary>
    public const int UInt32 = sizeof(uint);

    /// <summary>x86 pointer size — 4 bytes.</summary>
    public const int Pointer = UInt32;
}
