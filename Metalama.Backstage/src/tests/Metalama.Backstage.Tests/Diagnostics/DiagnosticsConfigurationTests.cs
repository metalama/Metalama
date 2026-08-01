// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tests.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Diagnostics;

/// <summary>
/// This tests class works with predefined default configuration set in constructor.
/// </summary>
public sealed class DiagnosticsConfigurationTests : TestsBase
{
    public DiagnosticsConfigurationTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void OutdatedConfiguration_DisablesLogging()
    {
        this.Time.Stop();

        ( IServiceProvider ServiceProvider, string FileName ) BuildServiceProvider( Action<Configuration.ConfigurationManager>? configure = null )
        {
            var serviceCollection = this.CloneServiceCollection();

            var configurationManager = new Configuration.ConfigurationManager( serviceCollection.BuildServiceProvider() );
            serviceCollection.AddSingleton<IConfigurationManager>( configurationManager );

            serviceCollection
                .AddSingleton<ITempFileManager>( new TempFileManager( serviceCollection.BuildServiceProvider() ) );

            configure?.Invoke( configurationManager );

            var serviceProviderBuilder = new ServiceCollectionBuilder( serviceCollection );

            serviceProviderBuilder.AddDiagnostics( ProcessKind.Other, new DiagnosticsInitializationOptions() );

            return (serviceCollection.BuildServiceProvider(), configurationManager.GetFilePath( typeof(DiagnosticsConfiguration) ));
        }

        // First: configure the logging.
        var (serviceProvider1, configFileName) = BuildServiceProvider(
            configurationManager => configurationManager.Update<DiagnosticsConfiguration>(
                _ => new DiagnosticsConfiguration()
                {
                    Logging = new LoggingConfiguration()
                    {
                        TraceCategories = ImmutableDictionary<string, bool>.Empty.Add( "*", true ),
                        Processes = ImmutableDictionary<string, bool>.Empty.Add( ProcessKind.Other.ToString(), true )
                    }
                } ) );

        // Make sure it actually logs.
        var logger1 = serviceProvider1.GetRequiredBackstageService<ILoggerFactory>().GetLogger( "Foo" );
        Assert.NotNull( logger1.Trace );
        Assert.True( this.FileSystem.FileExists( configFileName ) );

        // Move the clock 10 minutes later.
        this.Time.AddTime( TimeSpan.FromMinutes( 10 ) );
        var (serviceProvider2, _) = BuildServiceProvider();
        var logger2 = serviceProvider2.GetRequiredBackstageService<ILoggerFactory>().GetLogger( "Foo" );
        Assert.NotNull( logger2.Trace );

        // Move the clock 3 hours later.
        this.Time.AddTime( TimeSpan.FromHours( 3 ) );
        var (serviceProvider3, _) = BuildServiceProvider();
        var logger3 = serviceProvider3.GetRequiredBackstageService<ILoggerFactory>().GetLogger( "Foo" );
        Assert.Null( logger3.Trace );
    }

    private Configuration.ConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider );

    /// <summary>
    /// Verifies that a null value assigned through an object initializer or a <c>with</c> expression is normalized.
    /// </summary>
    /// <remarks>
    /// See issue #1777. These are the construction paths of the deserializer itself: the System.Text.Json source
    /// generator treats every <c>init</c> property as a constructor parameter and assigns all of them unconditionally,
    /// so JSON without a <c>logging</c> member overwrote the value set by the constructor with a null reference.
    /// </remarks>
    [Fact]
    public void Logging_WhenSetToNull_IsDefaultInstance()
    {
        var configuration = new DiagnosticsConfiguration { Logging = null! };

        Assert.NotNull( configuration.Logging );
        Assert.NotNull( configuration.Logging.Processes );
        Assert.NotNull( configuration.Logging.TraceCategories );

        var copy = configuration with { Logging = null! };

        Assert.NotNull( copy.Logging );
        Assert.NotNull( copy.Logging.Processes );
        Assert.NotNull( copy.Logging.TraceCategories );
    }

    /// <summary>
    /// Verifies that a non-default value is preserved, so that the normalization does not lose the configured logging.
    /// </summary>
    [Fact]
    public void Logging_WhenSet_IsPreserved()
    {
        var logging = new LoggingConfiguration { Processes = ImmutableDictionary<string, bool>.Empty.Add( ProcessKind.Compiler.ToString(), true ) };

        var configuration = new DiagnosticsConfiguration { Logging = logging };

        Assert.Same( logging, configuration.Logging );
        Assert.True( configuration.Logging.Processes[ProcessKind.Compiler.ToString()] );
    }

    /// <summary>
    /// Verifies that a <c>diagnostics.json</c> without a usable <c>logging</c> member is neither rejected nor discarded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>diagnostics.json</c> written by a version that had no <c>logging</c> section, or one the user edited with
    /// <c>metalama config edit diagnostics</c> and reduced to the sections they care about, simply has no <c>logging</c>
    /// entry. Reading it must yield a usable configuration rather than throw a <see cref="NullReferenceException"/> from
    /// <c>DiagnosticsConfiguration.Validate</c>.
    /// </para>
    /// <para>
    /// The assertion on <see cref="ConfigurationFile.Version"/> is the one that detects the defect of issue #1777.
    /// Asserting only that <c>Logging</c> is not null would pass even when the bug is present, because
    /// <c>ConfigurationManager.TryLoadConfigurationFile</c> catches every exception thrown by <c>Validate</c> and falls
    /// back to a fresh default instance, whose <c>Logging</c> is not null either. That fallback is the actual impact:
    /// the entire file was silently discarded and every setting was lost, not only the logging section, and the only
    /// trace was a log entry that this very failure may have disabled. A value read from the file and absent from a
    /// default instance is therefore what distinguishes the two outcomes.
    /// </para>
    /// <para>
    /// The version is the value asserted upon because it is the only one common to both cases, the second of which has
    /// no section other than <c>logging</c>. The <c>crashDumps</c> section of the first case comes from the report of
    /// issue #1777, where it illustrated that a section the user had written was lost as well.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData( """{ "version": 7, "crashDumps": { "processes": { "Compiler": true } } }""" )]
    [InlineData( """{ "version": 7, "logging": null }""" )]
    public void ReadConfigurationFile_WithoutLogging_IsNotDiscarded( string json )
    {
        var configurationManager = this.CreateConfigurationManager();
        var filePath = configurationManager.GetFilePath<DiagnosticsConfiguration>();
        this.FileSystem.WriteAllText( filePath, json );

        var configuration = configurationManager.Get<DiagnosticsConfiguration>();

        // The content of the file is preserved, which means that the file was not discarded.
        Assert.Equal( 7, configuration.Version );

        // The omitted section falls back to a usable default.
        Assert.NotNull( configuration.Logging );
        Assert.NotNull( configuration.Logging.Processes );
        Assert.NotNull( configuration.Logging.TraceCategories );
    }

    /// <summary>
    /// Verifies that a <c>diagnostics.json</c> file read through the <see cref="Configuration.ConfigurationManager"/>
    /// actually reaches every section of <see cref="DiagnosticsConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// This is the path taken by the workflows documented in
    /// <see href="https://doc.metalama.net/conceptual/configuration/process-dump"/> and
    /// <see href="https://doc.metalama.net/conceptual/configuration/profiling"/>, in which the user enables a process in
    /// the <c>crashDumps</c> or <c>profiling</c> section with <c>metalama config edit diagnostics</c>. See issue #1778.
    /// The file contains a <c>logging</c> section, like every file written by Metalama itself. A file without that
    /// section is covered by <see cref="ReadConfigurationFile_WithoutLogging_IsNotDiscarded"/>, the regression test of
    /// issue #1777.
    /// </remarks>
    [Fact]
    public void ConfigurationFile_AllSections_AreRead()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        var filePath = configurationManager.GetFilePath( typeof(DiagnosticsConfiguration) );

        this.FileSystem.CreateDirectory( Path.GetDirectoryName( filePath )! );

        this.FileSystem.WriteAllText(
            filePath,
            $$"""
              {
                "logging": { "processes": { "{{nameof(ProcessKind.Compiler)}}": false }, "trace": { "*": false } },
                "debugging": { "processes": { "{{nameof(ProcessKind.Compiler)}}": true } },
                "crashDumps": { "processes": { "{{nameof(ProcessKind.Compiler)}}": true }, "exceptionTypes": [ "System.InvalidOperationException" ] },
                "profiling": { "kind": "memory", "processes": { "{{nameof(ProcessKind.Compiler)}}": true } }
              }
              """ );

        var configuration = configurationManager.Get<DiagnosticsConfiguration>();

        Assert.True( configuration.Debugging.Processes[nameof(ProcessKind.Compiler)] );
        Assert.True( configuration.CrashDumps.Processes[nameof(ProcessKind.Compiler)] );
        Assert.Equal( "System.InvalidOperationException", Assert.Single( configuration.CrashDumps.ExceptionTypes ) );
        Assert.True( configuration.Profiling.Processes[nameof(ProcessKind.Compiler)] );
        Assert.Equal( "memory", configuration.Profiling.Kind );
    }
}