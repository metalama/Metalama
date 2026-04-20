// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// A context passed to constructors and <see cref="IInitializable.Initialize"/> methods to coordinate
/// post-initialization behavior. Carries the caller's intent, aspect behavior slots,
/// and optional metadata.
/// </summary>
/// <remarks>
/// <para>
/// This struct is 16 bytes on x64 (no wasted padding): a <see cref="CallerIntent"/> byte,
/// a <c>uint</c> slot bitmask, and an optional <see cref="InitializationMetadata"/> reference.
/// </para>
/// <para>
/// <c>default(InitializationContext)</c> is valid and equivalent to <see cref="Default"/>:
/// <see cref="Intent"/> is <see cref="CallerIntent.None"/>, no slots, no metadata.
/// </para>
/// </remarks>
public readonly struct InitializationContext
{
    private readonly CallerIntent _intent;
    private readonly uint _slots;
    private readonly InitializationMetadata? _metadata;

    private InitializationContext( CallerIntent intent, uint slots = 0u, InitializationMetadata? metadata = null )
    {
        this._intent = intent;
        this._slots = slots;
        this._metadata = metadata;
    }

    /// <summary>
    /// The default context — <see cref="CallerIntent.None"/>, no slots, no metadata.
    /// </summary>
    public static InitializationContext Default { get; } = new( CallerIntent.None );

    /// <summary>
    /// A context signaling that the caller will call <see cref="IInitializable.Initialize"/> after construction
    /// (e.g., after an object initializer).
    /// </summary>
    public static InitializationContext WillInitialize { get; } = new( CallerIntent.WillInitialize );

    /// <summary>
    /// A context for <c>with</c> expressions or clone operations. The
    /// <see cref="IInitializable.Initialize"/> method (typically invoked via
    /// <see cref="InitializableExtensions.WithInitialize{T}"/>) should revalidate invariants
    /// and reinitialize derived state.
    /// </summary>
    public static InitializationContext Modify { get; } = new( CallerIntent.None, 0u, InitializationMetadata.Modify );

    /// <summary>
    /// Creates a context with the given metadata. Used when calling <see cref="IInitializable.Initialize"/>
    /// directly (not via a constructor), e.g., after deserialization or a <c>with</c> expression.
    /// The resulting context has <see cref="CallerIntent.WillInitialize"/>, signaling to descendants
    /// that <see cref="IInitializable.Initialize"/> is being invoked.
    /// </summary>
    public static InitializationContext Create( InitializationMetadata metadata ) => new( CallerIntent.WillInitialize, 0u, metadata );

    /// <summary>
    /// The caller's intent regarding <see cref="IInitializable.Initialize"/> invocation.
    /// </summary>
    public CallerIntent Intent => this._intent;

    /// <summary>
    /// Whether <see cref="IInitializable.Initialize"/> will be called by the caller.
    /// <c>true</c> when <see cref="Intent"/> is <see cref="CallerIntent.WillInitialize"/>.
    /// </summary>
    public bool WillCallOnInitialized => this._intent != CallerIntent.None;

    /// <summary>
    /// Optional metadata describing the initialization context. Typically a singleton.
    /// Returns <c>null</c> for default construction (equivalent to
    /// <see cref="InitializationMetadata.Default"/>).
    /// </summary>
    public InitializationMetadata? Metadata => this._metadata;

    /// <summary>
    /// Returns <c>true</c> when the given <see cref="InitializationSlot"/> is set in this context, meaning a
    /// derived type has guaranteed it will handle the corresponding concern itself. Aspects whose templates
    /// share a slot use this to skip work that a more-derived layer has already taken responsibility for.
    /// </summary>
    public bool IsHandled( InitializationSlot slot ) => (this._slots & slot.Mask) != 0;

    /// <summary>
    /// Returns a copy of the current context with the given slots added to the handled set, suitable for
    /// passing to <c>base.Initialize(...)</c> from a derived <see cref="IInitializable.Initialize"/> override.
    /// Each slot tells the base layer that the derived type will handle the corresponding concern itself.
    /// </summary>
    /// <param name="slots">The aspect-coordination slots to mark as handled. Combine multiple slots with the
    /// <c>|</c> operator on <see cref="InitializationSlot"/>.</param>
    /// <remarks>
    /// <see cref="Intent"/> is normalized to <see cref="CallerIntent.WillInitialize"/> to preserve the promise
    /// that <see cref="IInitializable.Initialize"/> is being invoked, and <see cref="Metadata"/> is propagated
    /// unchanged from the current context.
    /// </remarks>
    public InitializationContext Descend( InitializationSlot slots = default ) => new( CallerIntent.WillInitialize, this._slots | slots.Mask, this._metadata );
}