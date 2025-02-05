// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

namespace Metalama.Backstage.Diagnostics;

[ConfigurationFile( "diagnostics.json", EnvironmentVariableName = EnvironmentVariableName )]
[Description( "Logging and debugging options of Metalama itself." )]
public record DiagnosticsConfiguration : ConfigurationFile
{
    public const string EnvironmentVariableName = "METALAMA_DIAGNOSTICS";

    [JsonProperty( "logging" )]
    public LoggingConfiguration Logging { get; init; } = new();

    [JsonProperty( "debugging" )]
    public DebuggerConfiguration Debugging { get; } = new();

    [JsonProperty( "crashDumps" )]
    public CrashDumpConfiguration CrashDumps { get; } = new();

    [JsonProperty( "profiling" )]
    public ProfilingConfiguration Profiling { get; } = new();

    public DiagnosticsConfiguration()
    {
        var processes = Enum.GetValues( typeof(ProcessKind) )
            .Cast<ProcessKind>()
            .ToImmutableDictionary( x => x.ToString(), _ => false );

        this.Logging = new LoggingConfiguration
        {
            Processes = processes,
            TraceCategories = ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase ).Add( "*", false )
        };

        this.Debugging = new DebuggerConfiguration() { Processes = processes };

        this.Profiling = new ProfilingConfiguration() { Kind = "performance", Processes = processes };

        this.CrashDumps = new CrashDumpConfiguration() { Processes = processes, ExceptionTypes = ImmutableArray.Create( "*" ) };
    }

    public override void Validate( Action<string> reportWarning )
    {
        base.Validate( reportWarning );

        void ValidateProcessKinds( IEnumerable<string> processKinds, string path )
        {
            foreach ( var processKind in processKinds )
            {
                if ( !Enum.TryParse<ProcessKind>( processKind, out _ ) )
                {
                    reportWarning(
                        $"Invalid key '{processKind}' at path '{path}'. Valid keys are: {string.Join( ", ", Enum.GetNames( typeof(ProcessKind) ) )}" );
                }
            }
        }

        ValidateProcessKinds( this.Logging.Processes.Keys, "logging.processes" );
        ValidateProcessKinds( this.Debugging.Processes.Keys, "debugging.processes" );
        ValidateProcessKinds( this.CrashDumps.Processes.Keys, "crashDumps.processes" );
    }
}