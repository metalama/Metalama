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

            serviceProviderBuilder.AddDiagnostics( ProcessKind.Other );

            return (serviceCollection.BuildServiceProvider(), configurationManager.GetFilePath( typeof(DiagnosticsConfiguration) ));
        }

        // First: configure the logging.
        var (serviceProvider1, fileName) = BuildServiceProvider(
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

        Assert.True( this.FileSystem.FileExists( fileName ) );

        // Move the clock 3 hours later.
        this.Time.AddTime( TimeSpan.FromHours( 3 ) );

        var (serviceProvider2, _) = BuildServiceProvider();

        var logger2 = serviceProvider2.GetRequiredBackstageService<ILoggerFactory>().GetLogger( "Foo" );
        Assert.Null( logger2.Trace );
    }
}