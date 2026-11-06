// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Diagnostics;

/// <summary>
/// Reports an exception caught at the boundary of a thread, a timer callback or an event raised by the operating system,
/// where letting it escape would terminate the process. See https://github.com/metalama/Metalama/issues/2317.
/// </summary>
/// <remarks>
/// <para>
/// This service is at the bottom of the dependency graph: its implementation resolves nothing when it is constructed, so
/// that the most basic services, such as <see cref="Infrastructure.IFileSystem"/> and
/// <see cref="Infrastructure.IDateTimeProvider"/>, can depend on it even though the telemetry services that it reports to
/// depend on them in turn.
/// </para>
/// <para>
/// The exception is reported through <see cref="Telemetry.ITelemetryService"/> with the default policy when that service is
/// registered, and only logged otherwise.
/// </para>
/// </remarks>
[PublicAPI]
public interface IBackgroundExceptionReporter : IBackstageService
{
    /// <summary>
    /// Logs <paramref name="exception"/> and, when it is an error, reports it. Never throws.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="logger">The logger, or <see langword="null"/>.</param>
    /// <param name="boundary">A description of the boundary at which the exception was caught, used in the log.</param>
    void Report( Exception exception, ILogger? logger, string boundary );
}
