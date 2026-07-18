// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Utilities;

public static class ProcessUtilities
{
    private static readonly bool _isCurrentProcessUnattended;
    private static readonly BufferingLoggerFactory _isCurrentProcessUnattendedLog = new();

    static ProcessUtilities()
    {
        // It is critical to perform this detection early when the process is started, and to remember the result,
        // because the parent process may end before the current process ends, and we would lose the ability
        // to walk the parent processes.
        // Therefore we must remember the result in a static field.
        _isCurrentProcessUnattended = IsCurrentProcessUnattendedCore( _isCurrentProcessUnattendedLog );
    }

    [PublicAPI]
    public static ProcessKind ProcessKind { get; } = GetProcessKind();

    private static ProcessKind GetProcessKind()
    {
        // Note that the same logic is duplicated in Metalama.Framework.CompilerExtensions.ProcessKindHelper and cannot 
        // be shared. Any change here must be done there too.

        var processName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();

        switch ( processName )
        {
            case "devenv":
                return ProcessKind.DevEnv;

            case "servicehub.roslyncodeanalysisservice":
            case "servicehub.roslyncodeanalysisservices":
            case "devhub":
                return ProcessKind.RoslynCodeAnalysisService;

            case "servicehub.host":
                {
                    var commandLine = Environment.CommandLine.ToLowerInvariant();

#pragma warning disable CA1307
                    if ( commandLine.Contains( "$codelensservice$" ) )
                    {
                        return ProcessKind.CodeLensService;
                    }
                    else
                    {
                        return ProcessKind.Other;
                    }
#pragma warning restore CA1307
                }

            case "visualstudio":
                return ProcessKind.VisualStudioMac;

            case "csc":
            case "vbcscompiler":
                return ProcessKind.Compiler;

            case "resharpertestrunner":
            case "resharpertestrunner64":
                return ProcessKind.ResharperTestRunner;

            case "microsoft.codeanalysis.languageserver":
            case "microsoft.visualstudio.code.languageserver":
                return ProcessKind.LanguageServer;

            case "msbuild":
                return ProcessKind.MsBuild;

            case "testhost":
                return ProcessKind.TestHost;

            case "dotnet":
                {
                    var commandLine = Environment.CommandLine.ToLowerInvariant();

#pragma warning disable CA1307
                    if ( commandLine.Contains( "jetbrains.resharper.roslyn.worker" ) ||
                         commandLine.Contains( "jetbrains.roslyn.worker" ) )
                    {
                        return ProcessKind.Rider;
                    }
                    else if ( commandLine.Contains( "vbcscompiler.dll" ) || commandLine.Contains( "csc.dll" ) )
                    {
                        return ProcessKind.Compiler;
                    }
                    else if ( commandLine.Contains( "languageserver.dll" ) )
                    {
                        return ProcessKind.LanguageServer;
                    }
                    else if ( commandLine.Contains( "omnisharp.dll" ) )
                    {
                        return ProcessKind.OmniSharp;
                    }
                    else if ( commandLine.Contains( "resharpertestrunner.dll" ) )
                    {
                        return ProcessKind.ResharperTestRunner;
                    }
                    else if ( commandLine.Contains( "msbuild.dll" ) )
                    {
                        return ProcessKind.MsBuild;
                    }
                    else if ( commandLine.Contains( "dotnet-format.dll" ) )
                    {
                        return ProcessKind.Format;
                    }
                    else
                    {
                        return ProcessKind.Other;
                    }
#pragma warning restore CA1307
                }

            default:
                if ( processName.StartsWith( "linqpad", StringComparison.Ordinal ) )
                {
                    return ProcessKind.LinqPad;
                }
                else
                {
                    return ProcessKind.Other;
                }
        }
    }

    [PublicAPI]
    public static bool IsCurrentProcessUnattended( ILoggerFactory loggerFactory )
    {
        _isCurrentProcessUnattendedLog.Replay( loggerFactory );

        return _isCurrentProcessUnattended;
    }

    /// <summary>
    /// Parent process names that indicate an unattended (CI / build server) environment.
    /// </summary>
    private static readonly HashSet<string> _unattendedParentProcesses = new()
    {
        "services",
        "java",               // TeamCity, Atlassian Bamboo (can also be "bamboo"), Jenkins, GoCD
        "bamboo",             // Atlassian Bamboo
        "agent.worker",       // Azure Pipelines
        "runner.worker",      // GitHub Actions
        "buildkite-agent",    // BuildKite
        "circleci-agent",     // CircleCI (Docker, but has specific process name)
        "agent",              // Semaphore CI (Linux)
        "sshd: travis [priv]" // Travis CI (Linux)
    };

    /// <summary>
    /// Parent process names that indicate an attended environment even when a parent process would otherwise suggest
    /// an unattended one.
    /// </summary>
    private static readonly HashSet<string> _notUnattendedParentProcesses = new()
    {
        "rider" // Rider needs to be checked, because it can have Java as its parent process.
    };

    /// <summary>
    /// Gets a value indicating whether the given <paramref name="processKind"/> is an interactive Visual Studio host
    /// process. This includes the main Visual Studio process (<c>devenv</c>) and its out-of-process Roslyn analysis
    /// host (<c>ServiceHub.RoslynCodeAnalysisService</c>, <c>DevHub</c>).
    /// </summary>
    /// <remarks>
    /// These processes are always part of an interactive user session, even though the operating system reports the
    /// out-of-process analysis host as non-interactive (it runs in a non-interactive window station / session 0).
    /// They must therefore not be classified as unattended, otherwise telemetry (including exception reports) is
    /// silently disabled in them. See issue #1729.
    /// </remarks>
    internal static bool IsInteractiveIdeProcess( ProcessKind processKind )
        => processKind is ProcessKind.DevEnv or ProcessKind.RoslynCodeAnalysisService;

    private static bool IsCurrentProcessUnattendedCore( ILoggerFactory loggerFactory )
    {
        var logger = loggerFactory.GetLogger( nameof(ProcessUtilities) );

        var processKind = ProcessKind;

        // Determine whether the current process runs in a non-interactive session. This is intentionally evaluated the
        // same way as before (the checks are mutually exclusive and short-circuit): a non-GUI session, a Linux Docker
        // container, or session 0 on Windows.
        var isNonInteractiveSession = false;

        if ( !Environment.UserInteractive )
        {
            logger.Trace?.Log( "Non-interactive session detected because Environment.UserInteractive = false." );

            isNonInteractiveSession = true;
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        {
            if ( IsRunningInDockerContainer( logger ) )
            {
                logger.Trace?.Log( "Non-interactive session detected because of Docker containerized environment." );

                isNonInteractiveSession = true;
            }
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            if ( Environment.OSVersion.Version.Major >= 6 && Process.GetCurrentProcess().SessionId == 0 )
            {
                logger.Trace?.Log( "Non-interactive session detected because SessionId = 0 on Windows." );

                isNonInteractiveSession = true;
            }
        }

        // The parent-process heuristics are only meaningful for interactive sessions (to detect a CI agent, or to
        // whitelist Rider). When the session itself is non-interactive there is no need to enumerate the parents, and
        // skipping it preserves the previous behavior of returning immediately without the enumeration (which could
        // otherwise fail). Interactive IDE host processes short-circuit to attended inside IsProcessUnattended.
        if ( isNonInteractiveSession )
        {
            return IsProcessUnattended( processKind, isNonInteractiveSession: true, Array.Empty<string>(), logger );
        }

        IReadOnlyList<ProcessInfo> parentProcesses;

        try
        {
            parentProcesses = GetParentProcesses( logger, _unattendedParentProcesses );
        }
        catch ( Exception e )
        {
            // An interactive IDE host is attended regardless of whether parent enumeration succeeded.
            if ( IsInteractiveIdeProcess( processKind ) )
            {
                logger.Trace?.Log( $"Attended mode assumed for the interactive IDE host process '{processKind}'." );

                return false;
            }

            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) || RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
            {
                logger.Warning?.Log( $"Unattended mode detected because the detection was not successful: {e}" );

                return true;
            }
            else
            {
                logger.Error?.Log( $"Unattended mode detected because the detection was not successful: {e}" );

                return false;
            }
        }

        if ( logger.Trace != null )
        {
            logger.Trace?.Log( "Parent processes:" );

            foreach ( var process in parentProcesses )
            {
                logger.Trace?.Log( process.ImagePath == null ? $"- Unknown process ID {process.ProcessId}" : $"- {process.ProcessName}: {process.ImagePath}" );
            }
        }

        var parentProcessNames = parentProcesses.Where( p => p.ProcessName != null ).Select( p => p.ProcessName! ).ToArray();

        return IsProcessUnattended( processKind, isNonInteractiveSession: false, parentProcessNames, logger );
    }

    /// <summary>
    /// Determines whether a process is unattended, given its <paramref name="processKind"/>, whether it runs in a
    /// non-interactive session, and the names of its parent processes.
    /// </summary>
    /// <remarks>
    /// This is the testable core of the unattended detection: <see cref="IsCurrentProcessUnattendedCore"/> gathers
    /// these inputs from the current process and delegates to this method. An interactive Visual Studio host process
    /// (see <see cref="IsInteractiveIdeProcess"/>) is always attended, even when it runs in a non-interactive session,
    /// which is why the out-of-process Roslyn analysis host must be handled here. See issue #1729.
    /// </remarks>
    internal static bool IsProcessUnattended(
        ProcessKind processKind,
        bool isNonInteractiveSession,
        IReadOnlyList<string> parentProcessNames,
        ILogger? logger = null )
    {
        logger ??= NullLogger.Instance;

        if ( IsInteractiveIdeProcess( processKind ) )
        {
            logger.Trace?.Log( $"Attended mode assumed for the interactive IDE host process '{processKind}'." );

            return false;
        }

        if ( isNonInteractiveSession )
        {
            logger.Trace?.Log( "Unattended mode detected because the process runs in a non-interactive session." );

            return true;
        }

        var notUnattendedProcessName = parentProcessNames.FirstOrDefault( p => _notUnattendedParentProcesses.Contains( p ) );

        if ( notUnattendedProcessName != null )
        {
            logger.Trace?.Log( $"Unattended mode NOT detected because of parent process '{notUnattendedProcessName}'." );

            return false;
        }

        var unattendedProcessName = parentProcessNames.FirstOrDefault( p => _unattendedParentProcesses.Contains( p ) );

        if ( unattendedProcessName != null )
        {
            logger.Trace?.Log( $"Unattended mode detected because of parent process '{unattendedProcessName}'." );

            return true;
        }

        logger.Trace?.Log( "Unattended mode NOT detected." );

        return false;
    }

    /// <summary>
    /// Gets the parent processes of the current process.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pivots">List of process names, that will stop the search when encountered. This helps to improve the performance.</param>
    /// <returns>The list of parent processes of the current process.</returns>
    [PublicAPI]
    public static IReadOnlyList<ProcessInfo> GetParentProcesses( ILogger? logger = null, ISet<string>? pivots = null )
    {
        logger ??= NullLogger.Instance;

        ParentProcessSearchBase parentProcessSearch;

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            parentProcessSearch = new ParentProcessSearchWindows( logger );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
        {
            parentProcessSearch = new ParentProcessSearchLinux( logger );
        }
        else if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
        {
            parentProcessSearch = new ParentProcessSearchMac( logger );
        }
        else
        {
            throw new NotSupportedException( "Getting parent processes in not supported on the current platform." );
        }

        return parentProcessSearch.GetParentProcesses( pivots );
    }

    private static bool IsRunningInDockerContainer( ILogger logger )
    {
        string? ReadFileSafe( string path )
        {
            try
            {
                return File.ReadAllText( path );
            }
            catch ( Exception e )
            {
                logger.Trace?.Log( $"Could not read '{path}' file: {e.Message}" );

                return null;
            }
        }

        // If the process is running inside a Docker container
        // init (pid '1') process control group collection will have /docker/ as a part of the groups hierarchies.
        var process1ControlGroup = ReadFileSafe( "/proc/1/cgroup" );

        if ( !string.IsNullOrEmpty( process1ControlGroup ) )
        {
            if ( process1ControlGroup.ContainsOrdinal( "docker" ) )
            {
                logger.Trace?.Log( "Running inside a Docker container based on process control group." );

                return true;
            }
        }

        var process1Environment = ReadFileSafe( "/proc/1/environ" );

        if ( !string.IsNullOrEmpty( process1Environment ) )
        {
            if ( process1Environment.ContainsOrdinal( "container=lxc" ) )
            {
                logger.Trace?.Log( "Running inside a Docker container based on LXC container init process." );

                return true;
            }

            if ( process1Environment.ContainsOrdinal( "container=docker" ) )
            {
                logger.Trace?.Log( "Running inside a Docker container based on Docker container init process." );

                return true;
            }

            // On Azure DevOps, the previous conditions aren't met, and we use the following condition instead.
            if ( process1Environment.ContainsOrdinal( "DOTNET_RUNNING_IN_CONTAINER=true" ) )
            {
                logger.Trace?.Log( "Running inside a container based on sh dotnet startup process." );

                return true;
            }
        }

        logger.Trace?.Log( "Not running inside a Docker container." );

        return false;
    }
}