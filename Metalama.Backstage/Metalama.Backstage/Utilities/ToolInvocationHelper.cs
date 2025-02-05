// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

// ReSharper disable AccessToDisposedClosure

namespace Metalama.Backstage.Utilities;

[PublicAPI]
public static class ToolInvocationHelper
{
    public static bool InvokeTool(
        ILogger logger,
        string fileName,
        string commandLine,
        string? workingDirectory,
        ToolInvocationOptions? options = null )
    {
        if ( !InvokeTool(
                logger,
                fileName,
                commandLine,
                workingDirectory,
                out var exitCode,
                options ) )
        {
            return false;
        }
        else if ( exitCode != 0 )
        {
            logger.Error?.Log( $"The process `\"{fileName}\" {commandLine}` failed with exit code {exitCode}." );

            return false;
        }
        else
        {
            return true;
        }
    }

    public static bool InvokeTool(
        ILogger logger,
        string fileName,
        string commandLine,
        string? workingDirectory,
        out int exitCode,
        ToolInvocationOptions? options = null )
    {
        options ??= ToolInvocationOptions.Default;

        return InvokeTool(
            logger,
            fileName,
            commandLine,
            workingDirectory,
            default,
            out exitCode,
            s =>
            {
                if ( !string.IsNullOrWhiteSpace( s ) )
                {
                    logger.Trace?.Log( s );
                }
            },
            s =>
            {
                if ( !string.IsNullOrWhiteSpace( s ) )
                {
                    foreach ( var replace in options.ReplacePatterns )
                    {
                        if ( replace.Regex.IsMatch( s ) )
                        {
                            s = replace.Regex.Replace( s, replace.GetReplacement );

                            break;
                        }
                    }

                    if ( options.SilentPatterns.Any( p => p.IsMatch( s ) ) )
                    {
                        // Ignored.
                    }
                    else if ( options.ErrorPatterns.Any( p => p.IsMatch( s ) ) )
                    {
                        logger.Error?.Log( s );
                    }
                    else if ( options.WarningPatterns.Any( p => p.IsMatch( s ) ) )
                    {
                        logger.Warning?.Log( s );
                    }
                    else if ( options.SuccessPatterns.Any( p => p.IsMatch( s ) ) )
                    {
                        logger.Info?.Log( s );
                    }
                    else if ( options.ImportantMessagePatterns.Any( p => p.IsMatch( s ) ) )
                    {
                        logger.Info?.Log( s );
                    }
                    else
                    {
                        logger.Trace?.Log( s );
                    }
                }
            },
            options );
    }

    // #16205 We don't allow cancellation here because there's no other working way to wait for a process exit
    // than Process.WaitForExit() on .NET Core when capturing process output.
    public static bool InvokeTool(
        ILogger logger,
        string fileName,
        string commandLine,
        string workingDirectory,
        out int exitCode,
        out string output,
        ToolInvocationOptions? options = null )
    {
        StringBuilder stringBuilder = new();

        var success =
            InvokeTool(
                logger,
                fileName,
                commandLine,
                workingDirectory,
                null,
                out exitCode,
                s =>
                {
                    lock ( stringBuilder )
                    {
                        stringBuilder.Append( s );
                        stringBuilder.Append( '\n' );
                    }
                },
                s =>
                {
                    lock ( stringBuilder )
                    {
                        stringBuilder.Append( s );
                        stringBuilder.Append( '\n' );
                    }
                },
                options );

        output = stringBuilder.ToString();

        return success && exitCode == 0;
    }

    private static bool InvokeTool(
        ILogger logger,
        string fileName,
        string commandLine,
        string? workingDirectory,
        CancellationToken? cancellationToken,
        out int exitCode,
        Action<string> handleErrorData,
        Action<string> handleOutputData,
        ToolInvocationOptions? options = null )
    {
        exitCode = 0;
        options ??= new ToolInvocationOptions();
        var processShouldRetry = false;
        var retryAttempts = 3;

        for ( var attempt = 0; attempt < retryAttempts; attempt++ )
        {
#pragma warning disable CA1307 // There is no string.Contains that takes a StringComparison
            if ( fileName.Contains( new string( Path.DirectorySeparatorChar, 1 ) ) && !File.Exists( fileName ) )
            {
                logger.Error?.Log( $"Cannot execute \"{fileName}\": file not found." );

                return false;
            }
#pragma warning restore CA1307

            const int restartLimit = 3;
            var restartCount = 0;
        start:

            ProcessStartInfo startInfo =
                new()
                {
                    FileName = fileName,
                    Arguments = Environment.ExpandEnvironmentVariables( commandLine ),
                    WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                    ErrorDialog = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

            if ( options.EnvironmentVariables != null )
            {
                foreach ( var pair in options.EnvironmentVariables )
                {
                    startInfo.Environment[pair.Key] = pair.Value;
                }
            }

            // Some environment variables must not be passed from the current process to the child process.
            foreach ( var blockedEnvironmentVariable in options.BlockedEnvironmentVariables )
            {
                startInfo.Environment[blockedEnvironmentVariable] = null;
            }

            // Filters process output where matching RegEx value indicates process failure.
            void FilterProcessOutput( string output )
            {
                if ( options.Retry is { Regex: { } } && options.Retry.Regex.IsMatch( output ) )
                {
                    processShouldRetry = true;
                }
            }

            Process process = new() { StartInfo = startInfo };

            using ( ManualResetEvent stdErrorClosed = new( false ) )
            using ( ManualResetEvent stdOutClosed = new( false ) )
            {
                process.ErrorDataReceived += ( _, args ) =>
                {
                    try
                    {
                        if ( args.Data == null )
                        {
                            stdErrorClosed.Set();
                        }
                        else
                        {
                            FilterProcessOutput( args.Data );

                            handleErrorData( args.Data );
                        }
                    }
                    catch ( Exception e )
                    {
                        logger.Error?.Log( e.ToString() );
                    }
                };

                process.OutputDataReceived += ( _, args ) =>
                {
                    try
                    {
                        if ( args.Data == null )
                        {
                            stdOutClosed.Set();
                        }
                        else
                        {
                            FilterProcessOutput( args.Data );

                            handleOutputData( args.Data );
                        }
                    }
                    catch ( Exception e )
                    {
                        logger.Error?.Log( e.ToString() );
                    }
                };

                // Log the command line, but not the one with expanded environment variables, so we don't expose secrets.
                if ( !options.Silent )
                {
                    logger.Info?.Log( $"{process.StartInfo.FileName} {commandLine}" );
                }

                using ( process )
                {
                    try
                    {
                        process.Start();
                    }
                    catch ( Win32Exception e ) when ( (uint) e.NativeErrorCode == 0x80004005 )
                    {
                        if ( restartCount < restartLimit )
                        {
                            logger.Warning?.Log(
                                "Access denied when starting a process. This might be caused by an anti virus software. Waiting 1000 ms and restarting." );

                            Thread.Sleep( 1000 );
                            restartCount++;

                            goto start;
                        }

                        throw;
                    }

                    if ( !cancellationToken.HasValue )
                    {
                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();
                        process.WaitForExit();
                    }
                    else
                    {
                        using ( ManualResetEvent cancelledEvent = new( false ) )
                        using ( ManualResetEvent exitedEvent = new( false ) )
                        {
                            process.EnableRaisingEvents = true;
                            process.Exited += ( _, _ ) => exitedEvent.Set();

                            using ( cancellationToken.Value.Register( () => cancelledEvent.Set() ) )
                            {
                                process.BeginErrorReadLine();
                                process.BeginOutputReadLine();

                                if ( !process.HasExited )
                                {
                                    var signal = WaitHandle.WaitAny( new WaitHandle[] { exitedEvent, cancelledEvent } );

                                    if ( signal == 1 )
                                    {
                                        cancellationToken.Value.ThrowIfCancellationRequested();
                                    }
                                }
                            }
                        }
                    }

                    // We will wait for a while for all output to be processed.
                    if ( !cancellationToken.HasValue )
                    {
                        WaitHandle.WaitAll( new WaitHandle[] { stdErrorClosed, stdOutClosed }, 10000 );
                    }
                    else
                    {
                        var i = 0;

                        while ( !WaitHandle.WaitAll( new WaitHandle[] { stdErrorClosed, stdOutClosed }, 100 ) &&
                                i++ < 100 )
                        {
                            cancellationToken.Value.ThrowIfCancellationRequested();
                        }
                    }

                    exitCode = process.ExitCode;

                    // We will retry if the process output indicates we should retry and exit code indicates failure.
                    if ( processShouldRetry && options.Retry != null && exitCode == options.Retry.ExitCode )
                    {
                        logger.Warning?.Log( "Build failed. Retrying." );

                        continue;
                    }

                    return true;
                }
            }
        }

        return true;
    }
}