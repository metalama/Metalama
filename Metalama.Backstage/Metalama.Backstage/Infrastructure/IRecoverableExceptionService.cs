// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Infrastructure;

/// <summary>
/// A service providing information if a recoverable exception can be ignored.
/// </summary>
/// <remarks>
/// This is to allow recoverable failures not to disturb users, but being hit by PostSharp engineers at the same time.
/// </remarks>
public interface IRecoverableExceptionService : IBackstageService
{
    /// <summary>
    /// Gets a value indicating whether a recoverable exception can be ignored.
    /// </summary>
    /// <remarks>
    /// I all cases such failure should be logged.
    /// When <c>true</c>, the exception can be swallowed silently.
    /// When <c>false</c>, the exception should be thrown.
    /// </remarks>
    bool CanIgnore { get; }
}