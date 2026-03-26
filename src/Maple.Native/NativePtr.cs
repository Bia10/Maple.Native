namespace Maple.Native;

/// <summary>
/// A typed 32-bit virtual address — the C# equivalent of a C++ typed pointer.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NativePtr{T}"/> encodes intent at the type level: it records not only
/// <em>where</em> a value lives in the binary but also <em>what type</em> lives there.
/// This provides the same safety as a typed C++ pointer (versus a raw <c>void*</c>)
/// without requiring unsafe code at the call site.
/// </para>
/// <para>
/// The three core casting patterns in C++ and their equivalents here:
/// <code>
/// // C++:  ZThread* p = (ZThread*)addr;
/// NativePtr&lt;ZThread&gt; p = 0x00B12340u;           // implicit cast from uint
///
/// // C++:  uint raw = (uint)(void*)p;
/// uint raw = (uint)p;                              // explicit cast back to uint
///
/// // C++:  ZThread t = *p;
/// ZThread t = p.Read(view, ZThread.ReadFrom);      // out-of-process dereference
///
/// // C++:  ZThread t = *p;  (live process)
/// ZThread t = p.Reinterpret(static ptr => NativeCast.Reinterpret&lt;ZThread&gt;(ptr));
/// </code>
/// </para>
/// <para>
/// Null pointer semantics mirror C++: <see cref="IsNull"/> returns
/// <see langword="true"/> when <see cref="Address"/> is zero, and any read on a null
/// pointer will throw <see cref="ArgumentOutOfRangeException"/> (out-of-process)
/// or access-violate (in-process, <see cref="Reinterpret"/>).
/// </para>
/// </remarks>
/// <typeparam name="T">The type this pointer addresses.</typeparam>
public readonly struct NativePtr<T>
{
    /// <summary>The raw 32-bit virtual address.</summary>
    public uint Address { get; }

    /// <summary>
    /// <see langword="true"/> when <see cref="Address"/> is zero — the null pointer.
    /// </summary>
    public bool IsNull => Address == 0;

    /// <summary>The null pointer constant for <typeparamref name="T"/>.</summary>
    public static NativePtr<T> Null => default;

    /// <summary>Creates a typed pointer from a raw address.</summary>
    public NativePtr(uint address) => Address = address;

    // ── Implicit / explicit operators ─────────────────────────────────────────

    /// <summary>
    /// <b>Implicit cast</b> from <see cref="uint"/> — mirrors <c>ZThread* p = (ZThread*)addr</c>.
    /// Allows writing <c>NativePtr&lt;ZThread&gt; p = 0x00B12340u;</c> without an explicit constructor.
    /// </summary>
    public static implicit operator NativePtr<T>(uint address) => new(address);

    /// <summary>
    /// <b>Explicit cast</b> to <see cref="uint"/> — mirrors <c>(uint)(void*)ptr</c>.
    /// Explicit to require intent at the call site and prevent accidental address loss.
    /// </summary>
    public static explicit operator uint(NativePtr<T> ptr) => ptr.Address;

    // ── Out-of-process: read via image view ───────────────────────────────────

    /// <summary>
    /// Reads T at this address from an image view.
    /// Equivalent to dereferencing a C++ pointer: <c>ZThread t = *p;</c>.
    /// </summary>
    /// <param name="view">Image context used for address-to-offset translation.</param>
    /// <param name="reader">Type's <c>ReadFrom</c> method — e.g. <c>ZThread.ReadFrom</c>.</param>
    public T Read(NativeImageView view, Func<ReadOnlySpan<byte>, int, T> reader) => view.Cast(Address, reader);

    /// <summary>
    /// Attempts to read T at this address; returns <see langword="false"/> if the address
    /// is null or out of image bounds.
    /// Safe analogue of <see cref="Read"/> — never throws on address errors.
    /// </summary>
    public bool TryRead(NativeImageView view, Func<ReadOnlySpan<byte>, int, T> reader, out T result) =>
        view.TryCast(Address, reader, out result);

    /// <summary>
    /// Returns <see langword="true"/> if this address is non-null and falls within
    /// the image bounds.
    /// </summary>
    public bool IsInBounds(NativeImageView view) => !IsNull && view.Contains(Address);

    // ── In-process: live pointer dereference ──────────────────────────────────

    /// <summary>
    /// <b>In-process, UNSAFE</b> — dereferences this address as a live T pointer.
    /// Equivalent to C++ <c>*reinterpret_cast&lt;T*&gt;(ptr)</c>.
    /// </summary>
    /// <remarks>
    /// Only valid when:
    /// <list type="bullet">
    ///   <item>The analysis tool is running inside the same process as the GMS client.</item>
    ///   <item><typeparamref name="T"/> is <c>unmanaged</c> (no managed references).</item>
    ///   <item>The address is valid and points to a live T instance.</item>
    /// </list>
    /// For out-of-process analysis, use <see cref="Read"/> instead.
    /// </remarks>
    public unsafe T Reinterpret(Func<nint, T> converter)
    {
        // The converter in practice is: ptr => *(ZThread*)ptr
        // provided as a lambda so the caller controls the unsafe block boundary
        return converter((nint)Address);
    }

    // ── Arithmetic — typed pointer arithmetic ─────────────────────────────────

    /// <summary>
    /// Returns a new pointer advanced by <paramref name="byteOffset"/> bytes.
    /// Mirrors C++ pointer arithmetic when element stride is known.
    /// </summary>
    public NativePtr<T> Add(int byteOffset) => new(Address + (uint)byteOffset);

    // ── Equality ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(NativePtr<T> other) => Address == other.Address;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is NativePtr<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Address.GetHashCode();

    /// <summary>Pointer equality.</summary>
    public static bool operator ==(NativePtr<T> left, NativePtr<T> right) => left.Address == right.Address;

    /// <summary>Pointer inequality.</summary>
    public static bool operator !=(NativePtr<T> left, NativePtr<T> right) => left.Address != right.Address;

    /// <inheritdoc/>
    public override string ToString() => IsNull ? "nullptr" : $"0x{Address:X8}";
}
