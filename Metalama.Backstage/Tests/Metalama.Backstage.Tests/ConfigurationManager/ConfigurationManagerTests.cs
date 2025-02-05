// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Testing;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

public class ConfigurationManagerTests : TestsBase
{
    public ConfigurationManagerTests( ITestOutputHelper logger ) : base(
        logger,
        applicationInfo: new TestApplicationInfo() { IsLongRunningProcess = true } ) { }

    [Fact]
    public void InvalidJson()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        var fileName = configurationManager.GetFilePath<TestConfigurationFile>();
        this.FileSystem.WriteAllText( fileName, "not valid json" );

        // Reading the file should be successful.
        var configuration = configurationManager.Get<TestConfigurationFile>();
        Assert.NotNull( configuration.Timestamp );
        Assert.Contains( this.Log.Entries, e => e.Severity == TestLoggerFactory.Severity.Error );

        // Updating the file should be successful.
        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );
    }

    [Fact]
    public void VersionIsUpdated()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );

        var initialConfiguration = configurationManager.Get<TestConfigurationFile>();
        Assert.Null( initialConfiguration.Timestamp );
        Assert.Null( initialConfiguration.Version );

        configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } );

        var modifiedConfiguration = configurationManager.Get<TestConfigurationFile>();
        Assert.NotNull( modifiedConfiguration.Timestamp );
        Assert.Equal( 1, modifiedConfiguration.Version );
    }

    [Fact]
    public void OutsideModification()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        var gotEvent = new ManualResetEvent( false );

        // Make sure we retrieve the value first.
        _ = configurationManager.Get<TestConfigurationFile>();

        configurationManager.ConfigurationFileChanged += file =>
        {
            if ( file is TestConfigurationFile )
            {
                gotEvent.Set();
            }
        };

        var path = configurationManager.GetFilePath<TestConfigurationFile>();
        var newValue = new TestConfigurationFile() { IsModified = true };
        this.FileSystem.WriteAllText( path, newValue.ToJson() );

        Assert.True( gotEvent.WaitOne( 180000 ) );

        var newValueFromManager = configurationManager.Get<TestConfigurationFile>();

        Assert.True( newValueFromManager.IsModified );
    }
}