// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// Don't run this test on .Net Framework, to prevent concurrent execution of the .Net Framework and .Net tests.

#if NETCOREAPP
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

public sealed class ConfigurationManagerLoadTests : TestsBase
{
    public ConfigurationManagerLoadTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public async Task ConcurrentConditionalUpdateSucceedsOnlyOnce()
    {
        // For this test, we want to use the "real" file system, not the simulation.

        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>( new FileSystem() );
        services.AddSingleton<IDateTimeProvider>( this.Time );
        services.AddSingleton<IEnvironmentVariableProvider>( this.EnvironmentVariableProvider );
        services.AddSingleton<EarlyLoggerFactory>();
        services.AddSingleton<IStandardDirectories>( s => new StandardDirectories( s ) );
        var serviceProvider = services.BuildServiceProvider();

        for ( var i = 0; i < 50; i++ )
        {
            var configurationManager = new Configuration.ConfigurationManager( serviceProvider );
            configurationManager.Update<TestConfigurationFile>( f => f with { IsModified = false } );

            bool UpdateImpl()
            {
                var myConfigurationManager = new Configuration.ConfigurationManager( serviceProvider );

                return myConfigurationManager.UpdateIf<TestConfigurationFile>( f => !f.IsModified, f => f with { IsModified = true } );
            }

            var tasks = new[] { Task.Run( UpdateImpl ), Task.Run( UpdateImpl ) };

            await Task.WhenAll( tasks );

            var countTrue = tasks.Count( t => t.Result );
            Assert.Equal( 1, countTrue );
        }
    }
}
#endif