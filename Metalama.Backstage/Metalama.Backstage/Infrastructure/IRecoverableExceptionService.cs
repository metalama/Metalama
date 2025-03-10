// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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