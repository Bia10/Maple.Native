namespace Maple.Native;

/// <summary>
/// Extends <see cref="INativeAllocator"/> with the runtime primitives required for
/// lock-aware mutation of live Maple client objects.
/// </summary>
/// <remarks>
/// <para>
/// Maple's <c>ZFatalSection</c> uses the owning thread's TEB pointer as its lock owner token.
/// A runtime allocator therefore needs both atomic 32-bit compare/exchange and a way to
/// identify the current thread's TEB-equivalent token.
/// </para>
/// <para>
/// Synthetic allocators may provide a fake per-thread TEB token. Raw out-of-process memory
/// backends should normally stop at <see cref="INativeAllocator"/>; implementations of this
/// interface are expected to run inside the client process, or otherwise have a real way to
/// participate in native thread ownership semantics.
/// </para>
/// </remarks>
public interface INativeRuntimeAllocator : INativeAllocator
{
    /// <summary>
    /// Gets the TEB-equivalent token for the current thread.
    /// </summary>
    uint CurrentThreadTeb { get; }

    /// <summary>
    /// Atomically compares the 32-bit value at <paramref name="address"/> with
    /// <paramref name="expected"/> and, when equal, replaces it with <paramref name="desired"/>.
    /// </summary>
    /// <param name="address">Address of the 32-bit word to update.</param>
    /// <param name="expected">Expected current value.</param>
    /// <param name="desired">Replacement value written when the compare succeeds.</param>
    /// <param name="observed">The value observed at <paramref name="address"/>.</param>
    /// <returns><see langword="true"/> when the swap succeeded; otherwise <see langword="false"/>.</returns>
    bool CompareExchangeUInt32(uint address, uint expected, uint desired, out uint observed);

    /// <summary>
    /// Yields execution while spinning on a live client lock.
    /// </summary>
    void YieldThread();
}
