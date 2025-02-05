// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Diagnostics;

public class EnvironmentVariableConfigurationTests : TestsBase
{
    private readonly IConfigurationManager _configurationManager;

    public EnvironmentVariableConfigurationTests( ITestOutputHelper logger ) : base( logger )
    {
        this._configurationManager = this.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();
        var standardDirectories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();

        // Initialize local DiagnosticsConfiguration.
        this.FileSystem.CreateDirectory( standardDirectories.ApplicationDataDirectory );
        this.FileSystem.WriteAllText( this._configurationManager.GetFilePath( typeof(DiagnosticsConfiguration) ), new DiagnosticsConfiguration().ToJson() );

        // Set up environment variable DiagnosticsConfiguration.
        var environmentConfiguration = new DiagnosticsConfiguration
        {
            Logging = new LoggingConfiguration() { Processes = ImmutableDictionary<string, bool>.Empty.Add( ProcessKind.Compiler.ToString(), true ) }
        };

        this.EnvironmentVariableProvider.Environment.Add( DiagnosticsConfiguration.EnvironmentVariableName, environmentConfiguration.ToJson() );
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( new TestApplicationInfo() ) )
            .AddSingleton<IConfigurationManager>( serviceProvider => new Configuration.ConfigurationManager( serviceProvider ) );
    }

    [Fact]
    public void ExistingEnvironmentVariable_OverridesConfiguration()
    {
        var diagnosticsConfiguration = this._configurationManager.Get<DiagnosticsConfiguration>();

        Assert.True( diagnosticsConfiguration.Logging.Processes[ProcessKind.Compiler.ToString()] );
    }
}