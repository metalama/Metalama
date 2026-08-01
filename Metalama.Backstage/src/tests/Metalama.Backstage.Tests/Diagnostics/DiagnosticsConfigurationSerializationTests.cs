// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Tests.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Diagnostics;

/// <summary>
/// Regression tests verifying that every section of <c>diagnostics.json</c> reaches the corresponding property of
/// <see cref="DiagnosticsConfiguration"/>.
/// </summary>
/// <remarks>
/// See issue #1778. The <c>debugging</c>, <c>crashDumps</c> and <c>profiling</c> properties were declared without an
/// <c>init</c> accessor. System.Text.Json serializes a get-only property but cannot deserialize into it, and the three
/// section types are immutable records, so they could not be populated in place either. The corresponding sections of
/// the file were therefore read and then discarded, which silently disabled the process-dump, debugger-attachment and
/// profiling workflows documented in <see href="https://doc.metalama.net/conceptual/configuration/process-dump"/> and
/// <see href="https://doc.metalama.net/conceptual/configuration/profiling"/>.
/// </remarks>
public sealed class DiagnosticsConfigurationSerializationTests : JsonSerializationTestsBase
{
    /// <summary>
    /// A <c>diagnostics.json</c> in which every section holds a value that differs from the constructor default.
    /// </summary>
    private const string _allSectionsJson =
        """
        {
          "logging": {
            "processes": { "Compiler": true },
            "trace": { "Pipeline": true },
            "stopLoggingAfterHours": 4.5
          },
          "debugging": {
            "processes": { "Compiler": true }
          },
          "crashDumps": {
            "processes": { "Compiler": true },
            "exceptionTypes": [ "System.InvalidOperationException" ]
          },
          "profiling": {
            "kind": "memory",
            "processes": { "Compiler": true }
          }
        }
        """;

    public DiagnosticsConfigurationSerializationTests( ITestOutputHelper output ) : base( output ) { }

    /// <summary>
    /// Verifies that each section of the file is actually assigned to the corresponding property.
    /// </summary>
    [Fact]
    public void Deserialize_WithAllSections_PopulatesAllSections()
    {
        var configuration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( _allSectionsJson, this.JsonOptions )!;

        this.Output.WriteLine( JsonSerializer.Serialize( configuration, this.JsonOptions ) );

        // The 'logging' section is the one that already worked, and serves as a control.
        Assert.True( configuration.Logging.Processes["Compiler"] );
        Assert.True( configuration.Logging.TraceCategories["Pipeline"] );
        Assert.Equal( 4.5, (double?) configuration.Logging.StopLoggingAfterHours );

        Assert.True( configuration.Debugging.Processes["Compiler"] );

        Assert.True( configuration.CrashDumps.Processes["Compiler"] );
        Assert.Equal( "System.InvalidOperationException", Assert.Single( configuration.CrashDumps.ExceptionTypes ) );

        Assert.True( configuration.Profiling.Processes["Compiler"] );
        Assert.Equal( "memory", configuration.Profiling.Kind );
    }

    /// <summary>
    /// Verifies that a file in which a section is present but empty does not resurrect the constructor defaults.
    /// </summary>
    /// <remarks>
    /// The default value of <see cref="CrashDumpConfiguration.ExceptionTypes"/> is not empty, so this distinguishes a
    /// section that was really read from one that was replaced by a new instance.
    /// </remarks>
    [Fact]
    public void Deserialize_WithEmptyCrashDumpsSection_DoesNotUseConstructorDefault()
    {
        var configuration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( """{ "crashDumps": { } }""", this.JsonOptions )!;

        Assert.Empty( configuration.CrashDumps.ExceptionTypes );
    }

    /// <summary>
    /// Verifies that a file that omits a section keeps a non-null default instance for that section.
    /// </summary>
    /// <remarks>
    /// The System.Text.Json source generator treats every <c>init</c> property as a constructor parameter and assigns
    /// all of them unconditionally, so an omitted section would otherwise be set to <c>null</c>.
    /// </remarks>
    [Theory]
    [InlineData( "{}" )]
    [InlineData( """{ "logging": { "processes": { "Compiler": true } } }""" )]
    public void Deserialize_WithoutSections_YieldsNonNullSections( string json )
    {
        var configuration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( json, this.JsonOptions )!;

        AssertNoSectionIsNull( configuration );
    }

    /// <summary>
    /// Verifies that a section explicitly set to <c>null</c> is normalized into a default instance.
    /// </summary>
    /// <remarks>
    /// A user editing the file with <c>metalama config edit diagnostics</c> can write a null just as easily as omitting
    /// the section, and the configuration is read on every start of every Metalama process.
    /// </remarks>
    [Fact]
    public void Deserialize_WithNullSections_YieldsNonNullSections()
    {
        const string json =
            """
            {
              "logging": null,
              "debugging": null,
              "crashDumps": null,
              "profiling": null
            }
            """;

        var configuration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( json, this.JsonOptions )!;

        AssertNoSectionIsNull( configuration );
    }

    /// <summary>
    /// Verifies that the values of all sections survive a serialization round-trip through the configuration file
    /// serializer, which is the path taken when the file is written and read back.
    /// </summary>
    [Fact]
    public void Serialize_ThenDeserialize_PreservesAllSections()
    {
        var configuration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( _allSectionsJson, this.JsonOptions )!;

        var json = this.JsonService.Serialize( configuration, typeof(DiagnosticsConfiguration) );

        this.Output.WriteLine( json );

        var roundTripped = JsonSerializer.Deserialize<DiagnosticsConfiguration>( json, this.JsonOptions )!;

        Assert.True( roundTripped.Debugging.Processes["Compiler"] );
        Assert.True( roundTripped.CrashDumps.Processes["Compiler"] );
        Assert.Equal( "System.InvalidOperationException", Assert.Single( roundTripped.CrashDumps.ExceptionTypes ) );
        Assert.True( roundTripped.Profiling.Processes["Compiler"] );
        Assert.Equal( "memory", roundTripped.Profiling.Kind );
    }

    /// <summary>
    /// Verifies that the default <c>processes</c> of a section is matched without regard to case, both in a
    /// default-constructed configuration and in one deserialized from a file that omits the section.
    /// </summary>
    /// <remarks>
    /// Every <c>processes</c> property declares <see cref="StringComparer.OrdinalIgnoreCase"/> in its initializer, and
    /// the converter used when the member is present in the file preserves that comparer, so the shared default value
    /// has to use it as well. Otherwise a process kind is matched by case when the section is absent from the file, and
    /// without regard to case when it is present.
    /// </remarks>
    [Fact]
    public void DefaultProcesses_AreCaseInsensitive()
    {
        const string differentlyCasedProcessKind = "compiler";

        var defaultConfiguration = new DiagnosticsConfiguration();

        Assert.True( defaultConfiguration.Logging.Processes.ContainsKey( differentlyCasedProcessKind ) );
        Assert.True( defaultConfiguration.Debugging.Processes.ContainsKey( differentlyCasedProcessKind ) );
        Assert.True( defaultConfiguration.CrashDumps.Processes.ContainsKey( differentlyCasedProcessKind ) );
        Assert.True( defaultConfiguration.Profiling.Processes.ContainsKey( differentlyCasedProcessKind ) );

        var deserializedConfiguration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( "{}", this.JsonOptions )!;

        Assert.True( deserializedConfiguration.Logging.Processes.ContainsKey( differentlyCasedProcessKind ) );
        Assert.True( deserializedConfiguration.Debugging.Processes.ContainsKey( differentlyCasedProcessKind ) );
        Assert.True( deserializedConfiguration.CrashDumps.Processes.ContainsKey( differentlyCasedProcessKind ) );
        Assert.True( deserializedConfiguration.Profiling.Processes.ContainsKey( differentlyCasedProcessKind ) );
    }

    /// <summary>
    /// Verifies that an invalid process kind is reported for every section that has a <c>processes</c> member.
    /// </summary>
    /// <remarks>
    /// The <c>profiling</c> section was not validated, which was harmless while the section was discarded, but it means
    /// that a typo in a section that is now read would be silently ignored. The configuration is built in memory rather
    /// than deserialized, so that exactly one section holds an invalid value and the path reported in the warning is
    /// unambiguous.
    /// </remarks>
    [Theory]
    [InlineData( "logging.processes" )]
    [InlineData( "debugging.processes" )]
    [InlineData( "crashDumps.processes" )]
    [InlineData( "profiling.processes" )]
    public void Validate_WithInvalidProcessKind_ReportsWarning( string expectedPath )
    {
        const string invalidProcessKind = "NotAProcessKind";

        var processes = ImmutableDictionary<string, bool>.Empty
            .WithComparers( StringComparer.OrdinalIgnoreCase )
            .Add( invalidProcessKind, true );

        var configuration = expectedPath switch
        {
            "logging.processes" => new DiagnosticsConfiguration { Logging = new LoggingConfiguration { Processes = processes } },
            "debugging.processes" => new DiagnosticsConfiguration { Debugging = new DebuggerConfiguration { Processes = processes } },
            "crashDumps.processes" => new DiagnosticsConfiguration { CrashDumps = new CrashDumpConfiguration { Processes = processes } },
            "profiling.processes" => new DiagnosticsConfiguration { Profiling = new ProfilingConfiguration { Processes = processes } },
            _ => throw new ArgumentOutOfRangeException( nameof(expectedPath) )
        };

        var warnings = new List<string>();
        configuration.Validate( warnings.Add );

        foreach ( var warning in warnings )
        {
            this.Output.WriteLine( warning );
        }

        var reportedWarning = Assert.Single( warnings );
        Assert.Contains( invalidProcessKind, reportedWarning, StringComparison.Ordinal );
        Assert.Contains( expectedPath, reportedWarning, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that a file in which every process kind is valid does not produce a warning.
    /// </summary>
    [Fact]
    public void Validate_WithAllSections_ReportsNoWarning()
    {
        var configuration = JsonSerializer.Deserialize<DiagnosticsConfiguration>( _allSectionsJson, this.JsonOptions )!;

        var warnings = new List<string>();
        configuration.Validate( warnings.Add );

        Assert.Empty( warnings );
    }

    /// <summary>
    /// Asserts that no section is <c>null</c>.
    /// </summary>
    private static void AssertNoSectionIsNull( DiagnosticsConfiguration configuration )
    {
        Assert.NotNull( configuration.Logging );
        Assert.NotNull( configuration.Debugging );
        Assert.NotNull( configuration.CrashDumps );
        Assert.NotNull( configuration.Profiling );
    }
}
