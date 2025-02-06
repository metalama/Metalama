// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Contexts;

/// <summary>
/// Defines the minimal semantics of a logging context required by the <see cref="FlashtraceSource"/> class.
/// This interface is not intended to be implemented by end users of Flashtrace.
/// </summary>
[PublicAPI]
public interface ILoggingContext : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the context is currently disposed (contexts can be recycled, therefore the
    /// disposed state is not the final state).
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets an integer that is incremented every time the current instance is being recycled.
    /// </summary>
    int RecycleId { get; }

    /// <summary>
    /// Gets a value indicating whether the context represents an <c>async</c> method or an activity in an <c>async</c> method.
    /// </summary>
    bool IsAsync { get; }

    /// <summary>
    /// Gets a cross-process globally unique identifier for the current context.
    /// </summary>
    string? SyntheticId { get; }
}