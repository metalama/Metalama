// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Infrastructure;

[PublicAPI]
public interface IProcessExecutor : IBackstageService
{
    IProcess Start( ProcessStartInfo startInfo );

    /// <summary>
    /// Starts a process, waits for its completion, and gets the text that the process has written to its standard
    /// output.
    /// </summary>
    /// <param name="startInfo">The process to start. This method redirects its standard streams.</param>
    /// <param name="timeout">The time after which the method stops waiting for the completion of the process. The
    /// process is terminated when this time expires.</param>
    /// <param name="standardOutput">At output, the text written by the process to its standard output.</param>
    /// <returns><c>true</c> if the process completed within <paramref name="timeout"/> and returned the exit code
    /// zero, otherwise <c>false</c>.</returns>
    /// <remarks>
    /// The implementation terminates the process it has started, but it does not terminate the processes that this
    /// process has started itself.
    /// </remarks>
    bool TryReadStandardOutput( ProcessStartInfo startInfo, TimeSpan timeout, [NotNullWhen( true )] out string? standardOutput );
}
