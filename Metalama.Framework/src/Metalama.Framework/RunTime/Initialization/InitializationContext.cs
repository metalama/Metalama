// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// A context passed to constructors and <c>OnInitialized</c> methods to coordinate
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
    /// A context signaling that the caller will call <c>OnInitialized</c> after construction
    /// (e.g., after an object initializer). The constructor should not self-invoke.
    /// </summary>
    public static InitializationContext WillInitialize { get; } = new( CallerIntent.WillInitialize );

    /// <summary>
    /// A context signaling that the constructor should self-invoke <c>OnInitialized</c>
    /// at the end of its body. Used at call sites without object initializers.
    /// </summary>
    public static InitializationContext CallInitialize { get; } = new( CallerIntent.CallInitialize );

    /// <summary>
    /// Creates a context with the given metadata. Used when calling <c>OnInitialized</c> directly
    /// (not via a constructor), e.g., after deserialization or a <c>with</c> expression.
    /// </summary>
    public static InitializationContext Create( InitializationMetadata metadata )
        => new( CallerIntent.None, 0u, metadata );

    /// <summary>
    /// The caller's intent regarding <c>OnInitialized</c> invocation.
    /// </summary>
    public CallerIntent Intent => this._intent;

    /// <summary>
    /// Whether <c>OnInitialized</c> will be called (either by the caller or self-invoked
    /// by the constructor). <c>true</c> when <see cref="Intent"/> is
    /// <see cref="CallerIntent.WillInitialize"/> or <see cref="CallerIntent.CallInitialize"/>.
    /// </summary>
    public bool WillCallOnInitialized => this._intent != CallerIntent.None;

    /// <summary>
    /// Optional metadata describing the initialization context. Typically a singleton.
    /// Returns <c>null</c> for default construction (equivalent to
    /// <see cref="InitializationMetadata.Default"/>).
    /// </summary>
    public InitializationMetadata? Metadata => this._metadata;

    /// <summary>
    /// Returns whether the given aspect behavior is guaranteed by a derived type.
    /// </summary>
    public bool IsHandledBy( InitializationSlot slot ) => (this._slots & slot.Mask) != 0;

    /// <summary>
    /// Returns a copy with the given slots added to the handled set.
    /// Normalizes <see cref="Intent"/> to <see cref="CallerIntent.WillInitialize"/>
    /// (preserving the promise that <c>OnInitialized</c> will be called) and preserves
    /// <see cref="Metadata"/> from the original context.
    /// Used when a derived <c>OnInitialized</c> calls <c>base.OnInitialized</c> to propagate
    /// which aspect behaviors the derived type guarantees.
    /// </summary>
    public InitializationContext Descend( InitializationSlot slots = default )
        => new( CallerIntent.WillInitialize, this._slots | slots.Mask, this._metadata );
}
