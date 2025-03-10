// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Testing;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using LoggerFactory = Metalama.Backstage.Diagnostics.LoggerFactory;

namespace Metalama.Backstage.Tests.Diagnostics;

public sealed class LoggerFactoryTests : TestsBase
{
    public LoggerFactoryTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services.AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( new TestApplicationInfo() ) );
        services.AddSingleton<ITempFileManager>( serviceProvider => new TempFileManager( serviceProvider ) );
    }

    private LoggerFactory CreateLoggerFactory()
        => new(
            this.ServiceProvider,
            new DiagnosticsConfiguration()
            {
                Logging = new LoggingConfiguration()
                {
                    TraceCategories = ImmutableDictionary<string, bool>.Empty.Add( "*", true ),
                    Processes = ImmutableDictionary<string, bool>.Empty.Add( ProcessKind.Other.ToString(), true )
                }
            },
            ProcessKind.Other );

    [Fact]
    public void TestSequentialWrite()
    {
        this.Time.Set( new DateTime( 1978, 6, 16, 0, 0, 0, DateTimeKind.Utc ) );

        var loggerFactory = this.CreateLoggerFactory();

        var logger = loggerFactory.GetLogger( "Test" );

        var n = 10000;

        for ( var i = 0; i < n; i++ )
        {
            logger.Trace?.Log( i.ToString( CultureInfo.InvariantCulture ) );

            if ( i % 100 == 0 )
            {
                // This is to cause many tasks to start.
                Thread.Sleep( 10 );
            }
        }

        var file = this.FileSystem.Mock.AllFiles.Single( f => Path.GetExtension( f ) == ".log" );
        loggerFactory.Close();

        var allLines = this.FileSystem.ReadAllLines( file );

        Assert.Equal( n, allLines.Length );
    }

    [Fact]
    public void TestScope()
    {
        var loggerFactory = this.CreateLoggerFactory();

        using ( loggerFactory.EnterScope( "Scope1" ) )
        {
            loggerFactory.GetLogger( "Test" ).Trace?.Log( "InScope1" );
        }

        using ( loggerFactory.EnterScope( "Scope2" ) )
        {
            loggerFactory.GetLogger( "Test" ).Trace?.Log( "InScope2" );
        }

        loggerFactory.Close();

        var files = this.FileSystem.Mock.AllFiles.ToList();
        var allLines1 = this.FileSystem.ReadAllLines( files.Single( f => f.ContainsOrdinal( "Scope1" ) ) );
        Assert.Contains( "InScope1", allLines1.Last(), StringComparison.Ordinal );
        var allLines2 = this.FileSystem.ReadAllLines( files.Single( f => f.ContainsOrdinal( "Scope2" ) ) );
        Assert.Contains( "InScope2", allLines2.Last(), StringComparison.Ordinal );
    }
}