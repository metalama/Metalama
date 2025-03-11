// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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