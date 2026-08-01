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

    /// <summary>
    /// Gets the options of the logging of Metalama itself.
    /// </summary>
    [JsonPropertyName( "logging" )]
    public LoggingConfiguration Logging { get; init; } = CreateDefaultLogging();

    /// <summary>
    /// Gets the options that make a Metalama process wait for a debugger to be attached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property needs an <c>init</c> accessor. System.Text.Json serializes a get-only property, but it cannot
    /// deserialize into it, and <see cref="DebuggerConfiguration"/> is immutable, so it cannot be populated in place
    /// either. The section was therefore read from the file and then discarded. See #1778.
    /// </para>
    /// <para>
    /// The null value has to be normalized because the System.Text.Json source generator treats every <c>init</c>
    /// property as a constructor parameter and assigns all of them unconditionally, so a file that omits the section
    /// would otherwise set the property to <c>null</c>.
    /// </para>
    /// </remarks>
    [JsonPropertyName( "debugging" )]
    public DebuggerConfiguration Debugging
    {
        get => this._debugging;
        init => this._debugging = value ?? CreateDefaultDebugging();
    }

    /// <summary>
    /// Gets the options of the collection of crash dumps.
    /// </summary>
    /// <remarks>
    /// See the remarks of <see cref="Debugging"/> for the reason why this property has an <c>init</c> accessor and
    /// normalizes the null value.
    /// </remarks>
    [JsonPropertyName( "crashDumps" )]
    public CrashDumpConfiguration CrashDumps
    {
        get => this._crashDumps;
        init => this._crashDumps = value ?? CreateDefaultCrashDumps();
    }

    /// <summary>
    /// Gets the options of the profiling of Metalama processes.
    /// </summary>
    /// <remarks>
    /// See the remarks of <see cref="Debugging"/> for the reason why this property has an <c>init</c> accessor and
    /// normalizes the null value.
    /// </remarks>
    [JsonPropertyName( "profiling" )]
    public ProfilingConfiguration Profiling
    {
        get => this._profiling;
        init => this._profiling = value ?? CreateDefaultProfiling();
    }

    private readonly DebuggerConfiguration _debugging = CreateDefaultDebugging();
    private readonly CrashDumpConfiguration _crashDumps = CreateDefaultCrashDumps();
    private readonly ProfilingConfiguration _profiling = CreateDefaultProfiling();

    /// <summary>
    /// The default value of the <c>processes</c> member of every section: all known kinds of processes, disabled.
    /// </summary>
    /// <remarks>
    /// The comparer has to be specified explicitly. Every <c>processes</c> property declares
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> in its initializer, and a process kind written in the file is
    /// matched without regard to case, so a default built with the default (case-sensitive) comparer would make the
    /// lookup of a section absent from the file behave differently from the lookup of a section present in it.
    /// </remarks>
    private static readonly ImmutableDictionary<string, bool> _defaultProcesses = Enum.GetValues( typeof(ProcessKind) )
        .Cast<ProcessKind>()
        .ToImmutableDictionary( x => x.ToString(), _ => false, StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Creates the default value of the <see cref="Logging"/> property.
    /// </summary>
    private static LoggingConfiguration CreateDefaultLogging()
        => new()
        {
            Processes = _defaultProcesses,
            TraceCategories = ImmutableDictionary<string, bool>.Empty.WithComparers( StringComparer.OrdinalIgnoreCase ).Add( "*", false )
        };

    /// <summary>
    /// Creates the default value of the <see cref="Debugging"/> property.
    /// </summary>
    private static DebuggerConfiguration CreateDefaultDebugging() => new() { Processes = _defaultProcesses };

    /// <summary>
    /// Creates the default value of the <see cref="CrashDumps"/> property.
    /// </summary>
    private static CrashDumpConfiguration CreateDefaultCrashDumps()
        => new() { Processes = _defaultProcesses, ExceptionTypes = ImmutableArray.Create( "*" ) };

    /// <summary>
    /// Creates the default value of the <see cref="Profiling"/> property.
    /// </summary>
    private static ProfilingConfiguration CreateDefaultProfiling() => new() { Kind = "performance", Processes = _defaultProcesses };

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
        ValidateProcessKinds( this.Profiling.Processes.Keys, "profiling.processes" );
    }
}