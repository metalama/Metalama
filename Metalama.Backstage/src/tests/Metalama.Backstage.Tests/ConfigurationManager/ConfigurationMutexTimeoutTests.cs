// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Testing;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Verifies that an unavailable global configuration mutex degrades the configuration instead of failing the operation
/// during which the configuration happens to be read or written. See issue #1847.
/// </summary>
/// <remarks>
/// The mutex is genuinely held by <see cref="HeldConfigurationMutex"/>, rather than simulated, so that the whole
/// acquisition path is exercised. The configuration managers under test therefore use a short timeout, because
/// otherwise every test would wait for the thirty seconds of the production timeout.
/// </remarks>
public sealed class ConfigurationMutexTimeoutTests : TestsBase
{
    private const int _mutexTimeoutMilliseconds = 200;

    public ConfigurationMutexTimeoutTests( ITestOutputHelper logger ) : base( logger )
    {
        this.InitializationOptions = this.InitializationOptions with
        {
            AdditionalJsonTypeInfoResolvers = new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default }
        };
    }

    private Configuration.ConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider, _mutexTimeoutMilliseconds );

    private bool HasLoggedError => this.Log.Entries.Any( e => e.Severity == TestLoggerFactory.Severity.Error );

    [Fact]
    public void GetReturnsTheDefaultConfigurationWhenTheMutexCannotBeAcquired()
    {
        using ( var writer = this.CreateConfigurationManager() )
        {
            Assert.True( writer.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );
        }

        using var configurationManager = this.CreateConfigurationManager();

        using ( new HeldConfigurationMutex( this.ServiceProvider ) )
        {
            var configuration = configurationManager.Get<TestConfigurationFile>();

            // The file could not be read, so the caller gets the default configuration instead of an exception.
            Assert.False( configuration.IsModified );
            Assert.Null( configuration.Timestamp );
            Assert.True( this.HasLoggedError );
        }

        // The default configuration must not have been cached, so that the real one is read once the mutex has become
        // available again.
        Assert.True( configurationManager.Get<TestConfigurationFile>().IsModified );
    }

    [Fact]
    public void UpdateIsAbandonedWhenTheMutexCannotBeAcquired()
    {
        using var configurationManager = this.CreateConfigurationManager();

        using ( new HeldConfigurationMutex( this.ServiceProvider ) )
        {
            // The update reports that it did not happen, instead of throwing.
            Assert.False( configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );

            // The update is abandoned rather than retried: retrying would wait for the same unavailable mutex ten times
            // over, which is what the optimistic-concurrency retry limit reports.
            Assert.DoesNotContain( this.Log.Entries, e => e.Message.ContainsOrdinal( "Too many attempts" ) );
            Assert.True( this.HasLoggedError );
        }

        // Nothing was written, and updating works again once the mutex has become available.
        Assert.False( configurationManager.Get<TestConfigurationFile>( true ).IsModified );
        Assert.True( configurationManager.UpdateIf<TestConfigurationFile>( c => !c.IsModified, c => c with { IsModified = true } ) );
        Assert.True( configurationManager.Get<TestConfigurationFile>( true ).IsModified );
    }

    [Fact]
    public void CachedConfigurationIsStillReturnedWhenTheMutexCannotBeAcquired()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.True( configurationManager.Update<TestConfigurationFile>( c => c with { IsModified = true } ) );

        using ( new HeldConfigurationMutex( this.ServiceProvider ) )
        {
            // A read that is served by the cache does not need the mutex at all, so it is not degraded.
            Assert.True( configurationManager.Get<TestConfigurationFile>().IsModified );
        }
    }
}
