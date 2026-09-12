// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Metalama.Backstage.Utilities;

/// <summary>
/// Supplies the facts from which the detection methods of <see cref="ContinuousIntegrationDetector"/> recognize a
/// continuous integration server: the environment variables, the parent processes of the current process, and the
/// processes running on the machine.
/// </summary>
/// <remarks>
/// The two sets of process names are collected on first use and not in the constructor, because collecting them is
/// expensive. A detection method reads the environment variable of its server first, so a process that runs outside
/// of a continuous integration server never asks for a process name and pays for neither set.
/// </remarks>
internal sealed class ContinuousIntegrationContext
{
    private readonly IEnvironmentVariableProvider _environmentVariables;
    private readonly ILogger _logger;
    private readonly Lazy<HashSet<string>> _parentProcessNames;
    private readonly Lazy<HashSet<string>> _runningProcessNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContinuousIntegrationContext"/> class.
    /// </summary>
    /// <param name="environmentVariables">Reads the environment variables.</param>
    /// <param name="getParentProcessNames">Supplies the names of the parent processes of the current process.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="getRunningProcessNames">Supplies the names of the processes running on the machine, or
    /// <c>null</c> to enumerate the processes of the current machine. A test supplies this parameter.</param>
    public ContinuousIntegrationContext(
        IEnvironmentVariableProvider environmentVariables,
        Func<IEnumerable<string>> getParentProcessNames,
        ILogger logger,
        Func<IEnumerable<string>>? getRunningProcessNames = null )
    {
        this._environmentVariables = environmentVariables;
        this._logger = logger;
        this._parentProcessNames = new Lazy<HashSet<string>>( () => CreateNameSet( getParentProcessNames() ) );
        this._runningProcessNames = new Lazy<HashSet<string>>( () => CreateNameSet( this.GetRunningProcessNamesSafe( getRunningProcessNames ) ) );
    }

    /// <summary>
    /// Determines whether an environment variable is set to a value that does not deny the condition.
    /// </summary>
    /// <remarks>
    /// A tool that wants to deny the condition sets the variable to a negative value instead of removing it, so a
    /// negative value counts as an absent variable.
    /// </remarks>
    public bool IsEnvironmentVariableSet( string name )
    {
        var value = this._environmentVariables.GetEnvironmentVariable( name );

        return !string.IsNullOrWhiteSpace( value )
               && !string.Equals( value, "false", StringComparison.OrdinalIgnoreCase )
               && !string.Equals( value, "0", StringComparison.Ordinal );
    }

    /// <summary>
    /// Determines whether a process of one of the given names is a parent of the current process.
    /// </summary>
    public bool HasParentProcess( params string[] processNames ) => processNames.Any( this._parentProcessNames.Value.Contains );

    /// <summary>
    /// Determines whether a process of one of the given names is a parent of the current process or is running on the
    /// machine.
    /// </summary>
    /// <remarks>
    /// The whole machine is examined, and not only the parent processes, because the agent of a continuous integration
    /// server runs for the whole duration of the job while it is not always a parent of the current process: MSBuild
    /// reuses its worker nodes across invocations, and a reused node is reparented to the init process when the
    /// invocation that started it ends. See issue #1859.
    /// </remarks>
    public bool IsProcessRunning( params string[] processNames )
        => this.HasParentProcess( processNames ) || processNames.Any( this._runningProcessNames.Value.Contains );

    private static HashSet<string> CreateNameSet( IEnumerable<string> names ) => new( names, StringComparer.OrdinalIgnoreCase );

    private IEnumerable<string> GetRunningProcessNamesSafe( Func<IEnumerable<string>>? getRunningProcessNames )
    {
        try
        {
            return getRunningProcessNames == null ? GetRunningProcessNames() : getRunningProcessNames();
        }
        catch ( Exception e )
        {
            // The current process is then considered attended unless another fact proves the contrary. Reporting a
            // continuous integration server on the sole ground that the processes cannot be enumerated would make the
            // failure of the enumeration a way to obtain an unattended license.
            this._logger.Warning?.Log( $"Cannot enumerate the processes running on the machine: {e.Message}" );

            return [];
        }
    }

    private static IEnumerable<string> GetRunningProcessNames()
    {
        var processes = Process.GetProcesses();

        try
        {
            var names = new List<string>( processes.Length );

            foreach ( var process in processes )
            {
                try
                {
                    names.Add( process.ProcessName );
                }
                catch ( Exception )
                {
                    // The process has exited between the enumeration and the reading of its name, or the current user
                    // cannot read it. Another process identifies the server if there is one.
                }
            }

            return names;
        }
        finally
        {
            foreach ( var process in processes )
            {
                process.Dispose();
            }
        }
    }
}
