using System.Buffers.Binary;

namespace Maple.Native;

/// <summary>
/// Typed, address-aware view over a flat PE image.
/// </summary>
/// <remarks>
/// <para>
/// This is the fundamental "cast pointer to native type" context for offline PE analysis.
/// It holds the image buffer and load base address, and converts absolute virtual addresses
/// (the 32-bit pointer values seen in the binary) to file offsets before dispatching to
/// the typed <c>ReadFrom</c> methods on each native type.
/// </para>
/// <para>
/// <b>Single struct cast (C++ <c>(T*)addr</c>):</b>
/// <code>
/// var view = new NativeImageView(image, imageBase: 0x00400000u);
/// ZThread thread = view.Cast(0x00B12340u, ZThread.ReadFrom);
/// </code>
/// </para>
/// <para>
/// <b>Typed pointer — <see cref="NativePtr{T}"/> (<c>(T*)addr</c> with type safety):</b>
/// <code>
/// NativePtr&lt;ZThread&gt; ptr = 0x00B12340u;     // implicit from uint
/// ZThread thread = ptr.Read(view, ZThread.ReadFrom);
/// </code>
/// </para>
/// <para>
/// <b>Safe / <c>is</c>/<c>as</c> patterns:</b>
/// <code>
/// if (view.Is&lt;ZThread&gt;(addr))                           // bounds pre-flight
///     ZThread t = view.Cast(addr, ZThread.ReadFrom);
///
/// ZThread? t = view.As(addr, ZThread.ReadFrom);            // null on out-of-bounds
///
/// if (view.TryCast(addr, ZThread.ReadFrom, out var t)) { } // try-cast
/// </code>
/// </para>
/// <para>
/// <b>ZArray casts:</b>
/// <code>
/// ZXString[] strs = view.ReadZArrayOfZXString(payloadRva, count: 20);
/// ZThread[]  pool = view.ReadZArrayOfPtrs(payloadRva, ZThread.ReadFrom);
/// </code>
/// </para>
/// <para>
/// Assumes a flat PE mapping where RVA equals file offset relative to the image base —
/// valid for GMS v95 because the executable's section file-alignment matches its
/// virtual-address alignment.
/// </para>
/// </remarks>
public sealed class NativeImageView
{
    private readonly ReadOnlyMemory<byte> _image;

    /// <summary>Base load address of the PE image (typically <c>0x00400000</c> for GMS x86 clients).</summary>
    public uint ImageBase { get; }

    /// <summary>
    /// Creates a view over <paramref name="image"/> loaded at <paramref name="imageBase"/>.
    /// </summary>
    /// <param name="image">The full PE image bytes.</param>
    /// <param name="imageBase">Absolute load address of the first byte of <paramref name="image"/>.</param>
    public NativeImageView(ReadOnlyMemory<byte> image, uint imageBase)
    {
        _image = image;
        ImageBase = imageBase;
    }

    // ── Address translation ───────────────────────────────────────────────────

    /// <summary>
    /// Converts an absolute virtual address to a flat file offset.
    /// </summary>
    /// <param name="address">Absolute virtual address (a 32-bit pointer value from the binary).</param>
    /// <returns>File offset from the start of the image buffer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="address"/> is below the image base or beyond the end of the image.
    /// </exception>
    public int FileOffset(uint address)
    {
        if (address < ImageBase)
            throw new ArgumentOutOfRangeException(
                nameof(address),
                $"Address 0x{address:X8} is below image base 0x{ImageBase:X8}."
            );

        long offset = (long)address - (long)ImageBase;

        if (offset >= _image.Length)
            throw new ArgumentOutOfRangeException(
                nameof(address),
                $"Address 0x{address:X8} (offset 0x{offset:X}) exceeds image size 0x{_image.Length:X}."
            );

        return (int)offset;
    }

    // ── Core cast primitive ───────────────────────────────────────────────────

    /// <summary>
    /// Resolves <paramref name="address"/> to a file offset and calls <paramref name="reader"/>.
    /// This is the fundamental cast-pointer-to-native-type operation.
    /// </summary>
    /// <typeparam name="T">The native type to read.</typeparam>
    /// <param name="address">Absolute virtual address of the struct.</param>
    /// <param name="reader">
    ///   A <c>static ReadFrom(ReadOnlySpan&lt;byte&gt;, int)</c> method — e.g.
    ///   <c>ZThread.ReadFrom</c>, <c>CInPacket.ReadFrom</c>, <c>ZSocketBuffer.ReadFrom</c>.
    /// </param>
    /// <example>
    /// <code>
    /// ZThread thread = view.Cast(0x00B12340u, ZThread.ReadFrom);
    /// COutPacket pkt  = view.Cast(0x00C04200u, COutPacket.ReadFrom);
    /// </code>
    /// </example>
    public T Cast<T>(uint address, Func<ReadOnlySpan<byte>, int, T> reader) => reader(_image.Span, FileOffset(address));

    /// <summary>
    /// <see cref="NativePtr{T}"/> overload — resolves the typed pointer and reads T.
    /// </summary>
    public T Cast<T>(NativePtr<T> ptr, Func<ReadOnlySpan<byte>, int, T> reader) => Cast(ptr.Address, reader);

    // ── TryCast — non-throwing safe cast ──────────────────────────────────────

    /// <summary>
    /// Attempts to read T at <paramref name="address"/>; returns <see langword="false"/>
    /// if the address is null or outside the image bounds.
    /// Equivalent to C# <c>obj as T</c> / C++ <c>dynamic_cast</c> semantics.
    /// </summary>
    public bool TryCast<T>(uint address, Func<ReadOnlySpan<byte>, int, T> reader, out T result)
    {
        if (address == 0 || address < ImageBase)
        {
            result = default!;
            return false;
        }

        long offset = (long)address - (long)ImageBase;

        if (offset >= _image.Length)
        {
            result = default!;
            return false;
        }

        result = reader(_image.Span, (int)offset);
        return true;
    }

    /// <summary>
    /// <see cref="NativePtr{T}"/> overload of <see cref="TryCast{T}(uint,Func{ReadOnlySpan{byte},int,T},out T)"/>.
    /// </summary>
    public bool TryCast<T>(NativePtr<T> ptr, Func<ReadOnlySpan<byte>, int, T> reader, out T result) =>
        TryCast(ptr.Address, reader, out result);

    // ── Contains — bounds check ───────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="address"/> is non-null, at or above
    /// the image base, and the region <c>[address, address + <paramref name="sizeHint"/>)</c>
    /// falls within the image.
    /// </summary>
    /// <param name="address">Absolute virtual address to check.</param>
    /// <param name="sizeHint">
    ///   Number of bytes that must be readable starting at <paramref name="address"/>.
    ///   Defaults to 1 (any in-bounds address).
    /// </param>
    public bool Contains(uint address, int sizeHint = 1)
    {
        if (address == 0 || address < ImageBase)
            return false;

        long offset = (long)address - (long)ImageBase;
        return offset + sizeHint <= _image.Length;
    }

    // ── Is<T> — typed bounds pre-flight ──────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> if the image contains enough bytes at
    /// <paramref name="address"/> to hold a <typeparamref name="T"/>.
    /// Equivalent to a C++ <c>sizeof</c>-aware range check before casting.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="INativeSized.NativeSize"/> so the full struct region is validated.
    /// Combine with <see cref="Cast{T}(uint,Func{ReadOnlySpan{byte},int,T})"/> for a safe
    /// cast sequence:
    /// <code>
    /// if (view.Is&lt;ZThread&gt;(addr))
    ///     ZThread t = view.Cast(addr, ZThread.ReadFrom);
    /// </code>
    /// </remarks>
    public bool Is<T>(uint address)
        where T : INativeSized => Contains(address, T.NativeSize);

    /// <summary><see cref="NativePtr{T}"/> overload of <see cref="Is{T}(uint)"/>.</summary>
    public bool Is<T>(NativePtr<T> ptr)
        where T : INativeSized => Is<T>(ptr.Address);

    // ── As<T> — nullable cast ─────────────────────────────────────────────────

    /// <summary>
    /// Reads T at <paramref name="address"/>; returns <see langword="null"/> if the address
    /// is null or out of bounds instead of throwing.
    /// Equivalent to C# <c>expr as T</c>.
    /// </summary>
    public T? As<T>(uint address, Func<ReadOnlySpan<byte>, int, T> reader)
        where T : struct => TryCast(address, reader, out T result) ? result : null;

    /// <summary><see cref="NativePtr{T}"/> overload of <see cref="As{T}(uint,Func{ReadOnlySpan{byte},int,T})"/>.</summary>
    public T? As<T>(NativePtr<T> ptr, Func<ReadOnlySpan<byte>, int, T> reader)
        where T : struct => As<T>(ptr.Address, reader);

    // ── ZArray readers ────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a <c>ZArray&lt;T*&gt;</c> with an explicit element count.
    /// </summary>
    /// <remarks>
    /// Each pointer in the array is resolved via <see cref="FileOffset"/> and then passed
    /// to <paramref name="elementReader"/>. Use this for any <c>ZArray</c> whose elements
    /// are pointers to structs directly readable by a <c>ReadFrom</c> method.
    /// </remarks>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="payloadRva">
    ///   Absolute address of the array payload — the value stored in the <c>ZArray::a</c> field.
    /// </param>
    /// <param name="count">Number of elements to read.</param>
    /// <param name="elementReader">Called with <c>(image, resolvedFileOffset)</c> for each element pointer.</param>
    public T[] ReadZArrayOfPtrs<T>(uint payloadRva, int count, Func<ReadOnlySpan<byte>, int, T> elementReader)
    {
        var span = _image.Span;
        uint[] ptrs = ZArray.ReadPointerElements(span, FileOffset(payloadRva), count);
        var result = new T[count];

        for (int i = 0; i < count; i++)
            result[i] = elementReader(span, FileOffset(ptrs[i]));

        return result;
    }

    /// <summary>
    /// Reads a <c>ZArray&lt;T*&gt;</c> with the element count taken from the allocation header
    /// (the <c>int32</c> stored immediately before the payload pointer).
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="payloadRva">Absolute address of the array payload (<c>ZArray::a</c>).</param>
    /// <param name="elementReader">Called with <c>(image, resolvedFileOffset)</c> for each element pointer.</param>
    public T[] ReadZArrayOfPtrs<T>(uint payloadRva, Func<ReadOnlySpan<byte>, int, T> elementReader)
    {
        var span = _image.Span;
        int payloadOffset = FileOffset(payloadRva);
        int count = ZArray.ReadCount(span, payloadOffset);
        uint[] ptrs = ZArray.ReadPointerElements(span, payloadOffset, count);
        var result = new T[count];

        for (int i = 0; i < count; i++)
            result[i] = elementReader(span, FileOffset(ptrs[i]));

        return result;
    }

    // ── ZXString array readers (two-level indirection) ────────────────────────

    /// <summary>
    /// Reads a <c>ZArray&lt;ZXString&lt;char&gt;*&gt;</c> with an explicit element count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ZXString&lt;char&gt;</c> wraps a single <c>char* _m_pStr</c> field — two levels of
    /// indirection are involved:
    /// <list type="number">
    ///   <item>Array element → <c>ZXString*</c> struct address</item>
    ///   <item>Struct field <c>_m_pStr</c> → char payload address</item>
    /// </list>
    /// Both dereferences are resolved through <see cref="FileOffset"/> before
    /// calling <see cref="ZXString.ReadFrom"/>.
    /// </para>
    /// </remarks>
    /// <param name="payloadRva">Absolute address of the array payload (<c>ZArray::a</c>).</param>
    /// <param name="count">Number of ZXString elements to read.</param>
    public ZXString[] ReadZArrayOfZXString(uint payloadRva, int count)
    {
        var span = _image.Span;
        uint[] structPtrs = ZArray.ReadPointerElements(span, FileOffset(payloadRva), count);
        var result = new ZXString[count];

        for (int i = 0; i < count; i++)
        {
            int structOffset = FileOffset(structPtrs[i]);
            uint pStrRva = BinaryPrimitives.ReadUInt32LittleEndian(span[structOffset..]);
            result[i] = ZXString.ReadFrom(span, FileOffset(pStrRva));
        }

        return result;
    }

    /// <summary>
    /// Reads a <c>ZArray&lt;ZXString&lt;char&gt;*&gt;</c> with the count taken from the allocation header.
    /// </summary>
    /// <param name="payloadRva">Absolute address of the array payload (<c>ZArray::a</c>).</param>
    public ZXString[] ReadZArrayOfZXString(uint payloadRva)
    {
        var span = _image.Span;
        int payloadOffset = FileOffset(payloadRva);
        int count = ZArray.ReadCount(span, payloadOffset);
        return ReadZArrayOfZXString(payloadRva, count);
    }

    /// <summary>
    /// Reads a <c>ZArray&lt;ZXString&lt;unsigned short&gt;*&gt;</c> with an explicit element count.
    /// </summary>
    /// <remarks>
    /// Identical two-level indirection pattern to <see cref="ReadZArrayOfZXString(uint, int)"/>
    /// but decodes the payload as UTF-16LE via <see cref="ZXStringWide.ReadFrom"/>.
    /// </remarks>
    /// <param name="payloadRva">Absolute address of the array payload (<c>ZArray::a</c>).</param>
    /// <param name="count">Number of ZXStringWide elements to read.</param>
    public ZXStringWide[] ReadZArrayOfZXStringWide(uint payloadRva, int count)
    {
        var span = _image.Span;
        uint[] structPtrs = ZArray.ReadPointerElements(span, FileOffset(payloadRva), count);
        var result = new ZXStringWide[count];

        for (int i = 0; i < count; i++)
        {
            int structOffset = FileOffset(structPtrs[i]);
            uint pStrRva = BinaryPrimitives.ReadUInt32LittleEndian(span[structOffset..]);
            result[i] = ZXStringWide.ReadFrom(span, FileOffset(pStrRva));
        }

        return result;
    }

    /// <summary>
    /// Reads a <c>ZArray&lt;ZXString&lt;unsigned short&gt;*&gt;</c> with the count taken from the allocation header.
    /// </summary>
    /// <param name="payloadRva">Absolute address of the array payload (<c>ZArray::a</c>).</param>
    public ZXStringWide[] ReadZArrayOfZXStringWide(uint payloadRva)
    {
        var span = _image.Span;
        int payloadOffset = FileOffset(payloadRva);
        int count = ZArray.ReadCount(span, payloadOffset);
        return ReadZArrayOfZXStringWide(payloadRva, count);
    }
}
