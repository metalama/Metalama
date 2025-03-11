// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Flashtrace.Transactions;

/// <summary>
/// Represents the decision whether, why and how a transaction must be opened for an activity.
/// </summary>
[PublicAPI]
public readonly struct TransactionRequirement
{
    private const short _isSampledFlag = 1;
    private readonly short _flags;

    private short State
    {
        get;
    }

    internal TransactionRequirement( short state, bool isSampled )
    {
        this._flags = isSampled && state != 0 ? _isSampledFlag : (short) 0;
        this.State = state;
    }

    /// <summary>
    /// Gets a value indicating whether the transaction must be opened based on a sampling policy.
    /// </summary>
    public bool IsSampled => (this._flags & _isSampledFlag) == _isSampledFlag;

    /// <summary>
    /// Gets a value indicating whether the activity requires a transaction to be opened.
    /// </summary>
    public bool RequiresTransaction => this.State != 0;

    /// <summary>
    /// Gets a <see cref="TransactionRequirement"/> representing the request to open a transaction based on a sampling policy.
    /// </summary>
    public static TransactionRequirement SampledTransaction => new( -1, true );

    /// <summary>
    /// Gets a <see cref="TransactionRequirement"/> representing the request to open a transaction based on a deterministic policy (without sampling).
    /// </summary>
    public static TransactionRequirement DeterministicTransaction => new( -1, false );

    /// <summary>
    /// Gets a <see cref="TransactionRequirement"/> representing the absence of request to open a transaction.
    /// </summary>
    public static TransactionRequirement NoTransaction => new( 0, false );

    /// <summary>
    /// Returns a copy of the current <see cref="TransactionRequirement"/> but with a different value of <see cref="RequiresTransaction"/>.
    /// Specifically, this method preserves the <see cref="IsSampled"/> property if the <paramref name="value"/> parameter is <c>true</c>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public TransactionRequirement WithRequiresTransaction( bool value ) => new( value ? this.State : (short) 0, this.IsSampled );

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"RequiresTransaction={this.RequiresTransaction}, IsSampled={this.IsSampled}";
    }
}