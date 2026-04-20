// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// A strongly-typed bitmask representing a single aspect behavior slot for cross-layer coordination.
/// Each aspect type that needs coordination allocates one slot at class initialization time.
/// </summary>
/// <remarks>
/// <para>
/// Slots are needed only by aspect types that address the telescoping constructor problem —
/// i.e., aspects whose <see cref="IInitializable.Initialize"/> behavior must be skipped when a derived type
/// guarantees it will handle the same concern.
/// </para>
/// <para>
/// A maximum of 32 slots can be allocated per <see cref="InitializationSlotFactory"/> (or per
/// AppDomain when using the default factory via <see cref="Allocate"/>). In practice, very few
/// aspect types need slots (likely fewer than half a dozen in a typical application).
/// </para>
/// </remarks>
public readonly struct InitializationSlot
{
    private readonly uint _mask;

    internal InitializationSlot( uint mask )
    {
        this._mask = mask;
    }

    /// <summary>
    /// Combines two slots into one.
    /// </summary>
    public static InitializationSlot operator |( InitializationSlot a, InitializationSlot b ) => new( a._mask | b._mask );

    /// <summary>
    /// Gets the raw bitmask value. For internal engine use only.
    /// </summary>
    internal uint Mask => this._mask;

    private static readonly InitializationSlotFactory _defaultFactory = new();

    /// <summary>
    /// Slot used by the engine's <c>OnConstructed</c> mechanism to coordinate multi-level
    /// inheritance. Derived constructors descend their context with this slot before calling the
    /// base constructor; base constructors guard the <c>OnConstructed</c> call with
    /// <see cref="InitializationContext.IsHandled"/> against this slot. Not intended for direct
    /// use by aspects.
    /// </summary>
    public static InitializationSlot OnConstructed { get; } = _defaultFactory.Allocate();

    /// <summary>
    /// Allocates a new slot from the default global factory. Maximum 32 slots per AppDomain.
    /// </summary>
    /// <returns>A new <see cref="InitializationSlot"/> with a unique bit position.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the maximum of 32 slots has been exceeded.</exception>
    public static InitializationSlot Allocate() => _defaultFactory.Allocate();
}