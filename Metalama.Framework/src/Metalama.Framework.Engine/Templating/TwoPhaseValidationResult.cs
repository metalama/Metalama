// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// Represents the result of a two-phase validation operation.
/// </summary>
internal readonly struct TwoPhaseValidationResult : IDisposable
{
    private readonly CancellationTokenSource _phase2Cts;

    public TwoPhaseValidationResult( Task<bool> phase1, Task<bool> phase2, CancellationTokenSource phase2Cts )
    {
        this.Phase1 = phase1;
        this.Phase2 = phase2;
        this._phase2Cts = phase2Cts;
    }

    /// <summary>
    /// Gets the task for phase 1 validation (syntax trees with compile-time code).
    /// </summary>
    public Task<bool> Phase1 { get; }

    /// <summary>
    /// Gets the task for phase 2 validation (syntax trees without compile-time code).
    /// </summary>
    public Task<bool> Phase2 { get; }

    /// <summary>
    /// Cancels phase 2 validation.
    /// </summary>
    public void CancelPhase2() => this._phase2Cts.Cancel();

    /// <summary>
    /// Disposes the internal cancellation token source.
    /// </summary>
    public void Dispose() => this._phase2Cts.Dispose();
}
