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

    /// <summary>
    /// Verifies that a <c>diagnostics.json</c> file read through the <see cref="Configuration.ConfigurationManager"/>
    /// actually reaches every section of <see cref="DiagnosticsConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// This is the path taken by the workflows documented in
    /// <see href="https://doc.metalama.net/conceptual/configuration/process-dump"/> and
    /// <see href="https://doc.metalama.net/conceptual/configuration/profiling"/>, in which the user enables a process in
    /// the <c>crashDumps</c> or <c>profiling</c> section with <c>metalama config edit diagnostics</c>. See issue #1778.
    /// The file contains a <c>logging</c> section, like every file written by Metalama itself, because a file without
    /// that section is discarded as a whole. That defect is tracked by issue #1777.
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