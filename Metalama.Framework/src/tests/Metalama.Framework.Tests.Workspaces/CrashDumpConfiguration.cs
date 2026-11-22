// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Tests.Workspaces;

/// <summary>
/// Configures the .NET runtime to write a crash dump when a child process of the test process crashes.
/// </summary>
/// <remarks>
/// <para>
/// <c>MSBuildWorkspace</c> loads projects in a separate process, the Roslyn BuildHost. When the BuildHost crashes, the
/// workspace reports only "The BuildHost process is not responding" or "The BuildHost process exited", followed by the
/// output that the process wrote before the crash, which often does not contain the error. A dump of the BuildHost is
/// the only evidence of the cause.
/// </para>
/// <para>
/// The configuration uses the environment variables of the .NET runtime (<c>DOTNET_DbgEnableMiniDump</c> and related).
/// The BuildHost inherits them, because <c>BuildHostProcessManager</c> starts it with the environment of the current
/// process. The runtime reads these variables when a process starts, so they have no effect on the test process itself.
/// The runtime writes a dump only when a process terminates because of an unhandled exception or a fatal error.
/// It does not write a dump when a process exits normally or is killed by another process, and it does not apply to the
/// .NET Framework version of the BuildHost.
/// </para>
/// <para>
/// The dumps are written to the directory given by the <c>DumpDirectory</c> assembly metadata, which the project file sets
/// to the directory that TeamCity publishes as the <c>dumps</c> artifact. The configuration is skipped when
/// <c>DOTNET_DbgEnableMiniDump</c> is already set, so that a configuration chosen by the developer takes precedence.
/// </para>
/// </remarks>
internal static class CrashDumpConfiguration
{
    /// <summary>
    /// The value of <c>DOTNET_DbgMiniDumpType</c> that requests a dump with the heap, which is required to inspect managed objects.
    /// </summary>
    private const string _heapDumpType = "2";

    /// <summary>
    /// Sets the environment variables that enable crash dumps, before the first test runs.
    /// </summary>
#pragma warning disable CA2255 // The ModuleInitializer attribute should not be used in libraries: this is a test executable.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "DOTNET_DbgEnableMiniDump" ) ) )
        {
            return;
        }

        var dumpDirectory = typeof(CrashDumpConfiguration).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault( a => a.Key == "DumpDirectory" )
            ?.Value;

        if ( string.IsNullOrEmpty( dumpDirectory ) )
        {
            return;
        }

        dumpDirectory = Path.GetFullPath( dumpDirectory );

        // The runtime does not create the directory of the dump file.
        Directory.CreateDirectory( dumpDirectory );

        Environment.SetEnvironmentVariable( "DOTNET_DbgEnableMiniDump", "1" );
        Environment.SetEnvironmentVariable( "DOTNET_DbgMiniDumpType", _heapDumpType );

        // The runtime replaces %e with the name of the executable and %p with the process identifier.
        Environment.SetEnvironmentVariable( "DOTNET_DbgMiniDumpName", Path.Combine( dumpDirectory, "%e.%p.dmp" ) );
    }
}
