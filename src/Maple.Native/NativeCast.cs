using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Maple.Native;

/// <summary>
/// Core static cast primitives for native type analysis.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the C# counterparts to C++ cast operators:
/// </para>
/// <list type="table">
///   <listheader><term>C++</term><description>C# equivalent here</description></listheader>
///   <item><term><c>*reinterpret_cast&lt;T*&gt;(ptr)</c></term>
///         <description><see cref="Reinterpret{T}(ReadOnlySpan{byte}, int)"/> (out-of-process, unmanaged T)<br/>
///         <see cref="Reinterpret{T}(nint)"/> (in-process, unmanaged T)</description></item>
///   <item><term><c>static_cast&lt;T&gt;(expr)</c></term>
///         <description><see cref="NativeImageView"/> — field-by-field <c>ReadFrom</c></description></item>
///   <item><term><c>dynamic_cast&lt;T*&gt;(ptr)</c></term>
///         <description><see cref="TryCast{T}(ReadOnlySpan{byte},int,Func{ReadOnlySpan{byte},int,T},out T)"/> — returns false on failure</description></item>
///   <item><term><c>sizeof(T)</c></term>
///         <description><see cref="SizeOf{T}"/></description></item>
///   <item><term><c>typeid(T).name()</c></term>
///         <description><see cref="NameOf{T}"/></description></item>
///   <item><term><c>obj as T</c></term>
///         <description><see cref="As{T}(ReadOnlySpan{byte},int,Func{ReadOnlySpan{byte},int,T})"/></description></item>
///   <item><term><c>obj is T</c></term>
///         <description><see cref="Is{T}(ReadOnlySpan{byte},int)"/></description></item>
/// </list>
/// <para>
/// <b>Out-of-process</b> operations take a <c>ReadOnlySpan&lt;byte&gt;</c> image and a
/// pre-translated file offset. The high-level <see cref="NativeImageView"/> handles address
/// translation (virtual address → file offset) automatically.
/// </para>
/// <para>
/// <b>In-process</b> operations take an <c><see langword="nint"/></c> live pointer and
/// are <c>unsafe</c> — they require the analysis tool to be running inside the target process.
/// </para>
/// </remarks>
public static class NativeCast
{
    // ══════════════════════════════════════════════════════════════════════════
    // reinterpret_cast
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>Out-of-process <c>reinterpret_cast</c></b> — overlays the bytes of
    /// <paramref name="image"/> at <paramref name="offset"/> directly onto <typeparamref name="T"/>.
    /// Equivalent to <c>*reinterpret_cast&lt;T*&gt;(ptr)</c> applied to offline bytes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires <typeparamref name="T"/> to be <c>unmanaged</c> (no managed references) and
    /// requires its C# field layout to match the binary layout exactly — same field order,
    /// same field sizes, no conversions. This is satisfied by types whose fields are all
    /// primitive integers (<c>uint</c>, <c>int</c>, <c>ushort</c>, etc.).
    /// </para>
    /// <para>
    /// <b>Do not use</b> for types with C++ <c>int</c> fields mapped to C# <c>bool</c>
    /// (such as <see cref="COutPacket"/> and <see cref="CInPacket"/>), or for any type
    /// that uses field-level conversion in its <c>ReadFrom</c> method.
    /// Use <see cref="NativeImageView"/> with the type's <c>ReadFrom</c> for those.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Unmanaged struct whose C# layout matches the binary layout.</typeparam>
    /// <param name="image">Raw PE image or any byte buffer.</param>
    /// <param name="offset">File offset of the first byte of T.</param>
    public static T Reinterpret<T>(ReadOnlySpan<byte> image, int offset)
        where T : unmanaged => MemoryMarshal.Read<T>(image[offset..]);

    /// <summary>
    /// <b>In-process <c>reinterpret_cast</c></b> — reads <typeparamref name="T"/> from
    /// a live process pointer. Equivalent to <c>*reinterpret_cast&lt;T*&gt;(ptr)</c>.
    /// </summary>
    /// <remarks>
    /// UNSAFE — performs no null or alignment check. Prefer <see cref="SafeRead{T}"/>
    /// when the pointer may be invalid. Use only when the analysis tool is running inside
    /// the target process (injected DLL or debugger scenario).
    /// </remarks>
    /// <typeparam name="T">Unmanaged struct type.</typeparam>
    /// <param name="ptr">Live virtual address pointing at a T instance.</param>
    public static unsafe T Reinterpret<T>(nint ptr)
        where T : unmanaged => Unsafe.ReadUnaligned<T>((void*)ptr);

    // ══════════════════════════════════════════════════════════════════════════
    // Safe in-process read
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>In-process safe read</b> — null-checks <paramref name="ptr"/> before
    /// reading <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> without reading if <paramref name="ptr"/> is zero.
    /// Alignment is not validated; use only with pointers known to be suitably aligned for T.
    /// </remarks>
    public static unsafe bool SafeRead<T>(nint ptr, out T result)
        where T : unmanaged
    {
        if (ptr == 0)
        {
            result = default;
            return false;
        }

        result = Unsafe.ReadUnaligned<T>((void*)ptr);
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TryCast — out-of-process, field-by-field (dynamic_cast semantics)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>Out-of-process safe cast</b> — returns <see langword="false"/> if <paramref name="offset"/>
    /// is outside the image bounds. Equivalent to C++ <c>dynamic_cast</c>: non-throwing on failure.
    /// </summary>
    /// <remarks>
    /// Bounds is checked against <paramref name="offset"/> only (not against the full struct size).
    /// For a full size-aware pre-flight, call <see cref="Is{T}"/> first.
    /// </remarks>
    public static bool TryCast<T>(
        ReadOnlySpan<byte> image,
        int offset,
        Func<ReadOnlySpan<byte>, int, T> reader,
        out T result
    )
    {
        if (offset < 0 || offset >= image.Length)
        {
            result = default!;
            return false;
        }

        result = reader(image, offset);
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Is — bounds pre-flight (obj is T semantics)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>Bounds pre-flight — <c>obj is T</c></b> equivalent for native memory.
    /// Returns <see langword="true"/> if the image contains enough bytes at
    /// <paramref name="offset"/> to hold a <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="INativeSized.NativeSize"/> so the entire struct region is checked,
    /// not just the starting byte. Combine with <see cref="TryCast{T}"/> for a fully safe
    /// cast sequence:
    /// <code>
    /// if (NativeCast.Is&lt;ZThread&gt;(image, offset))
    ///     ZThread t = NativeCast.Reinterpret&lt;ZThread&gt;(image, offset);
    /// </code>
    /// </remarks>
    public static bool Is<T>(ReadOnlySpan<byte> image, int offset)
        where T : INativeSized => offset >= 0 && offset + T.NativeSize <= image.Length;

    // ══════════════════════════════════════════════════════════════════════════
    // As — null-returning cast (C# as semantics)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>Nullable cast — <c>expr as T</c></b> equivalent.
    /// Returns <see langword="null"/> if <paramref name="offset"/> is out of bounds
    /// instead of throwing. Returns the decoded value on success.
    /// </summary>
    public static T? As<T>(ReadOnlySpan<byte> image, int offset, Func<ReadOnlySpan<byte>, int, T> reader)
        where T : struct
    {
        if (offset < 0 || offset >= image.Length)
            return null;

        return reader(image, offset);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // sizeof / typeof equivalents
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the in-memory size of <typeparamref name="T"/> in bytes.
    /// Equivalent to C++ <c>sizeof(T)</c>.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="INativeSized.NativeSize"/> — a compile-time constant
    /// defined on each native type.
    /// </remarks>
    public static int SizeOf<T>()
        where T : INativeSized => T.NativeSize;

    /// <summary>
    /// Returns the C# type name of <typeparamref name="T"/>.
    /// Approximate equivalent of C++ <c>typeid(T).name()</c>.
    /// </summary>
    /// <remarks>
    /// Uses <c>Type.Name</c> which is preserved in AOT builds for concrete
    /// value types. The name returned is the C# class name, which matches the C++ type
    /// name for all types in this library.
    /// </remarks>
    public static string NameOf<T>() => typeof(T).Name;
}
