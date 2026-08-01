// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Diagnostics;

[ConfigurationFile( "diagnostics.json", EnvironmentVariableName = EnvironmentVariableName )]
[Description( "Logging and debugging options of Metalama itself." )]
public sealed record DiagnosticsConfiguration : ConfigurationFile
{
    public const string EnvironmentVariableName = "METALAMA_DIAGNOSTICS";

    private readonly LoggingConfiguration _logging = new();

    /// <summary>
    /// Gets the logging options.
    /// </summary>
    /// <remarks>
    /// The value is normalized to a default instance because a null value throws a <see cref="NullReferenceException"/>
    /// in <see cref="Validate"/>, which causes the whole configuration file to be discarded. A property initializer
    /// alone is not enough: the System.Text.Json source generator treats every <c>init</c> property as a constructor
    /// parameter and assigns it unconditionally, so JSON without a <c>logging</c> entry overwrites the initializer with
    /// a null value.
    /// </remarks>
    [JsonPropertyName( "logging" )]
    public LoggingConfiguration Logging
    {
        get => this._logging;
        init => this._logging = value ?? new LoggingConfiguration();
    }

    [JsonPropertyName( "debugging" )]
    public DebuggerConfiguration Debugging { get; } = new();

    [JsonPropertyName( "crashDumps" )]
    public CrashDumpConfiguration CrashDumps { get; } = new();

    [JsonPropertyName( "profiling" )]
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