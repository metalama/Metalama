// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Testing;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

public sealed class ConfigurationManagerTests : TestsBase
{
    public ConfigurationManagerTests( ITestOutputHelper logger ) : base(
        logger,
        applicationInfo: new TestApplicationInfo() { IsLongRunningProcess = true } )
    {
        this.InitializationOptions = this.InitializationOptions with
        {
            AdditionalJsonTypeInfoResolvers = new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default }
        };
    }

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
        var jsonService = this.ServiceProvider.GetRequiredBackstageService<IJsonSerializationService>();
        this.FileSystem.WriteAllText( path, jsonService.Serialize( newValue, typeof(TestConfigurationFile) ) );

        Assert.True( gotEvent.WaitOne( 30000 ) );

        var newValueFromManager = configurationManager.Get<TestConfigurationFile>();

        Assert.True( newValueFromManager.IsModified );
    }
}