// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Utilities;

/// <summary>
/// The exception thrown by <see cref="DotNetTool"/> and <see cref="MSBuildTool"/> when the child process fails, either
/// because it returned a non-zero exit code or because it did not complete within the allowed time.
/// </summary>
/// <remarks>
/// The console output of the child process is exposed as <see cref="Output"/> instead of being available only as prose
/// inside <see cref="Exception.Message"/>, so that the caller can analyse the failure and report it as a diagnostic.
/// See issue #1744.
/// </remarks>
[PublicAPI]
public sealed class ProcessFailedException : InvalidOperationException
{
    /// <summary>
    /// Gets the path of the executable that was started.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the command-line arguments that were passed to the process.
    /// </summary>
    public string Arguments { get; }

    /// <summary>
    /// Gets the working directory of the process.
    /// </summary>
    public string WorkingDirectory { get; }

    /// <summary>
    /// Gets the exit code of the process, or <c>null</c> when the process was terminated because it exceeded
    /// <see cref="Timeout"/>.
    /// </summary>
    public int? ExitCode { get; }

    /// <summary>
    /// Gets the time, in milliseconds, that the process was allowed to run.
    /// </summary>
    public int Timeout { get; }

    /// <summary>
    /// Gets the standard output and standard error of the process, one item per line.
    /// </summary>
    /// <remarks>
    /// The collection is empty when the process was terminated because of a timeout, because the output of an
    /// unfinished build is not meaningful.
    /// </remarks>
    public ImmutableArray<string> Output { get; }

    /// <summary>
    /// Gets a value indicating whether the process was terminated because it exceeded <see cref="Timeout"/>.
    /// </summary>
    public bool HasTimedOut => this.ExitCode == null;

    private ProcessFailedException(
        string message,
        string fileName,
        string arguments,
        string workingDirectory,
        int? exitCode,
        int timeout,
        ImmutableArray<string> output ) : base( message )
    {
        this.FileName = fileName;
        this.Arguments = arguments;
        this.WorkingDirectory = workingDirectory;
        this.ExitCode = exitCode;
        this.Timeout = timeout;
        this.Output = output;
    }

    internal static ProcessFailedException CreateTimeout( string fileName, string arguments, string workingDirectory, int timeout )
        => new(
            $"The process '{fileName} {arguments}' did not complete in {timeout / 1000f} s.",
            fileName,
            arguments,
            workingDirectory,
            null,
            timeout,
            ImmutableArray<string>.Empty );

    internal static ProcessFailedException CreateNonZeroExitCode(
        string fileName,
        string arguments,
        string workingDirectory,
        int exitCode,
        int timeout,
        ImmutableArray<string> output )
        => new(
            $"Error calling `\"{fileName}\" {arguments}` in `{workingDirectory}` returned {exitCode}. Process output:"
            + Environment.NewLine + Environment.NewLine + string.Join( Environment.NewLine, output ),
            fileName,
            arguments,
            workingDirectory,
            exitCode,
            timeout,
            output );
}
