namespace Maple.Native;

/// <summary>
/// Marks a native type as having a known, fixed in-memory size.
/// Enables the <c>sizeof</c> analogue: <see cref="NativeCast.SizeOf{T}"/>.
/// </summary>
/// <remarks>
/// Implement on each fixed-size value type by pointing <see cref="NativeSize"/>
/// at the corresponding layout constant:
/// <code>
/// public readonly struct ZThread : INativeSized
/// {
///     public static int NativeSize =&gt; ZThreadLayout.TotalBytes;
///     // ...
/// }
/// </code>
/// Variable-size types (e.g. <see cref="ZXString"/>) whose allocation length depends
/// on payload content should <b>not</b> implement this interface.
/// </remarks>
public interface INativeSized
{
    /// <summary>
    /// Total in-memory size of this type in bytes — the C++ <c>sizeof(T)</c> equivalent.
    /// </summary>
    static abstract int NativeSize { get; }
}
