// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// A class used by the <see cref="InvariantAttribute"/> aspect to count the number of times the <c>SuspendInvariants</c>
/// method has been invoked.
/// </summary>
[PublicAPI]
public sealed class InvariantSuspensionCounter
{
    private int _value;

    /// <summary>
    /// Decrements the counter and returns <c>true</c> if the counter is back to zero.
    /// Note that this does not verify invariants in this case.
    /// </summary>
    public bool Decrement() => Interlocked.Decrement( ref this._value ) == 0;

    /// <summary>
    /// Increments the counter.
    /// </summary>
    public void Increment() => Interlocked.Increment( ref this._value );

    /// <summary>
    /// Gets a value indicating whether the verification of invariants is currently suspended.
    /// </summary>
    public bool AreInvariantsSuspended => this._value > 0;
}