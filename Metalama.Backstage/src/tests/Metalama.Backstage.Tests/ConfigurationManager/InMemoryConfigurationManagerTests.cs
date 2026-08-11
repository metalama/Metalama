// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Testing;
using System.Collections.Concurrent;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Tests <see cref="InMemoryConfigurationManager"/> against the contract of
/// <see cref="IConfigurationManager"/>.
/// </summary>
/// <remarks>
/// This substitute is used by nearly every test of this assembly through <see cref="TestsBase"/>, so a divergence
/// between it and the real manager makes those tests assert something other than what the product does. It had no
/// test of its own until a review found that its version counter behaved differently from the real one.
/// </remarks>
public sealed class InMemoryConfigurationManagerTests : TestsBase
{
    public InMemoryConfigurationManagerTests( ITestOutputHelper logger ) : base( logger )
    {
        this.InitializationOptions = this.InitializationOptions with
        {
            AdditionalJsonTypeInfoResolvers = new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default }
        };
    }

    private InMemoryConfigurationManager CreateConfigurationManager() => new( this.ServiceProvider );

    /// <summary>
    /// Appends a mark to the accumulating record of the test configuration file.
    /// </summary>
    /// <param name="configurationManager">The manager.</param>
    /// <param name="mark">The mark to append.</param>
    /// <returns>The outcome of the update.</returns>
    private static ConfigurationUpdateOutcome AppendMark( IConfigurationManager configurationManager, string mark )
        => configurationManager.Update(
            typeof(TestConfigurationFile),
            currentValue => ((TestConfigurationFile) currentValue) with { Marks = ((TestConfigurationFile) currentValue).Marks + mark } );

    /// <summary>
    /// Verifies that a file that has never been written is returned as a default instance carrying no timestamp.
    /// </summary>
    [Fact]
    public void AFileThatWasNeverWrittenHasNoTimestamp()
    {
        using var configurationManager = this.CreateConfigurationManager();

        var value = configurationManager.Get<TestConfigurationFile>();

        Assert.Null( value.Timestamp );
        Assert.Null( value.Version );
        Assert.Equal( "", value.Marks );
    }

    /// <summary>
    /// Verifies that the version counts the updates made to the file.
    /// </summary>
    [Fact]
    public void TheVersionCountsTheUpdates()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );
        Assert.Equal( 1, configurationManager.Get<TestConfigurationFile>().Version );

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "b" ) );
        Assert.Equal( 2, configurationManager.Get<TestConfigurationFile>().Version );
    }

    /// <summary>
    /// Verifies that a transformation returning a fresh instance, which carries no version, does not take the
    /// version backwards.
    /// </summary>
    /// <remarks>
    /// The same defect as in the real manager, and the reason this class needed a test: resetting a configuration
    /// file is written as <c>_ => new T()</c>, and deriving the version from the transformed value rather than
    /// from the value read restarts the count at one. The version is the tiebreaker used when two values share a
    /// modification time, so a count that restarts makes a newer value compare as older.
    /// </remarks>
    [Fact]
    public void ATransformationReturningAFreshInstanceDoesNotTakeTheVersionBackwards()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );
        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "b" ) );

        Assert.Equal(
            ConfigurationUpdateOutcome.Updated,
            configurationManager.Update( typeof(TestConfigurationFile), _ => new TestConfigurationFile() ) );

        var value = configurationManager.Get<TestConfigurationFile>();
        Assert.Equal( "", value.Marks );
        Assert.Equal( 3, value.Version );
    }

    /// <summary>
    /// Verifies that a transformation that declines leaves the stored value untouched.
    /// </summary>
    [Fact]
    public void ADecliningTransformationStoresNothing()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        Assert.Equal(
            ConfigurationUpdateOutcome.Declined,
            configurationManager.Update( typeof(TestConfigurationFile), _ => null ) );

        var value = configurationManager.Get<TestConfigurationFile>();
        Assert.Equal( "a", value.Marks );
        Assert.Equal( 1, value.Version );
    }

    /// <summary>
    /// Verifies that a transformation producing the value already stored is reported as such and does not increment
    /// the version.
    /// </summary>
    [Fact]
    public void ATransformationThatChangesNothingStoresNothing()
    {
        using var configurationManager = this.CreateConfigurationManager();

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        Assert.Equal(
            ConfigurationUpdateOutcome.NoChange,
            configurationManager.Update( typeof(TestConfigurationFile), currentValue => currentValue ) );

        Assert.Equal( 1, configurationManager.Get<TestConfigurationFile>().Version );
    }

    /// <summary>
    /// Verifies that a change is announced, and that the handler runs with nothing held, so that it can update
    /// another configuration file exactly as it can with the real manager.
    /// </summary>
    [Fact]
    public void AChangeIsAnnouncedAndTheHandlerCanUpdateAnotherFile()
    {
        using var configurationManager = this.CreateConfigurationManager();

        var announced = new ConcurrentQueue<ConfigurationFile>();

        configurationManager.ConfigurationFileChanged += value =>
        {
            announced.Enqueue( value );

            if ( value is TestConfigurationFile )
            {
                configurationManager.Update<SecondTestConfigurationFile>( c => c with { IsModified = true } );
            }
        };

        Assert.Equal( ConfigurationUpdateOutcome.Updated, AppendMark( configurationManager, "a" ) );

        Assert.Contains( announced, value => value is TestConfigurationFile );
        Assert.Contains( announced, value => value is SecondTestConfigurationFile );
        Assert.True( configurationManager.Get<SecondTestConfigurationFile>().IsModified );
    }

    /// <summary>
    /// Verifies that a file supplied to the constructor is returned as it was given.
    /// </summary>
    [Fact]
    public void AFileSuppliedToTheConstructorIsReturned()
    {
        using var configurationManager = new InMemoryConfigurationManager(
            this.ServiceProvider,
            new TestConfigurationFile { Marks = "supplied" } );

        Assert.Equal( "supplied", configurationManager.Get<TestConfigurationFile>().Marks );
    }
}
