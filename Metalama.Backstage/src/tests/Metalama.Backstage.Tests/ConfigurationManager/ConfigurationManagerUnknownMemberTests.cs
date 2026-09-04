// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Serialization;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.ConfigurationManager;

/// <summary>
/// Tests that <see cref="Configuration.ConfigurationManager"/> keeps the members of a configuration file that the
/// running version does not declare, so that an older version does not destroy the content written by a newer one
/// (#1923).
/// </summary>
public sealed class ConfigurationManagerUnknownMemberTests : TestsBase
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationManagerUnknownMemberTests( ITestOutputHelper logger ) : base(
        logger,
        applicationInfo: new TestApplicationInfo { IsLongRunningProcess = true } )
    {
        this.InitializationOptions = this.InitializationOptions with
        {
            AdditionalJsonTypeInfoResolvers = new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default }
        };

        this._jsonOptions = BackstageJsonContext.CreateCombinedOptions(
            writeIndented: true,
            new IJsonTypeInfoResolver[] { TestConfigurationJsonContext.Default } );
    }

    [Fact]
    public void UpdatePreservesUnknownMembersAtEveryLevel()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        var initialDocument = this.WriteFileWithUnknownMembers<NestedTestConfigurationFile>( configurationManager );

        configurationManager.Update<NestedTestConfigurationFile>( c => c with { Counter = c.Counter + 1 } );

        var finalDocument = this.ReadFile<NestedTestConfigurationFile>( configurationManager );

        // The known member has the new value.
        Assert.Equal( 1, (int) finalDocument["Counter"]! );

        // Every member that the running version does not declare is still there.
        UnknownMemberJson.AssertUnknownMembersPreserved( initialDocument, finalDocument );
        Assert.True( UnknownMemberJson.CountUnknownMembers( initialDocument ) > 0 );
    }

    [Fact]
    public void SuccessiveUpdatesPreserveUnknownMembers()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        var initialDocument = this.WriteFileWithUnknownMembers<NestedTestConfigurationFile>( configurationManager );

        for ( var i = 1; i <= 3; i++ )
        {
            configurationManager.Update<NestedTestConfigurationFile>( c => c with { Counter = c.Counter + 1 } );

            var document = this.ReadFile<NestedTestConfigurationFile>( configurationManager );

            Assert.Equal( i, (int) document["Counter"]! );
            UnknownMemberJson.AssertUnknownMembersPreserved( initialDocument, document );
        }
    }

    [Fact]
    public void UpdateOfRealConfigurationFilePreservesUnknownMembers()
    {
        // The test above uses a configuration file declared by the test project. This one uses a file of the
        // product, so that the members of ToastNotificationsConfiguration and of the values of its 'notifications'
        // dictionary are covered as they are shipped.
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        var initialDocument = this.WriteFileWithUnknownMembers<ToastNotificationsConfiguration>( configurationManager );

        var snoozeUntil = new DateTime( 2026, 3, 1, 0, 0, 0, DateTimeKind.Utc );

        configurationManager.Update<ToastNotificationsConfiguration>( c => c with { LastNotificationTime = snoozeUntil } );

        var finalDocument = this.ReadFile<ToastNotificationsConfiguration>( configurationManager );

        Assert.Equal( snoozeUntil, finalDocument["lastNotificationTime"]!.GetValue<DateTime>() );
        UnknownMemberJson.AssertUnknownMembersPreserved( initialDocument, finalDocument );
    }

    [Fact]
    public void ReadingFileWithUnknownMembersReportsNoWarningAndNoError()
    {
        var configurationManager = new Configuration.ConfigurationManager( this.ServiceProvider );
        _ = this.WriteFileWithUnknownMembers<NestedTestConfigurationFile>( configurationManager );

        // The entries logged while the services were built are not the subject of this test.
        var entriesBeforeRead = this.Log.Entries.Count;

        var configuration = configurationManager.Get<NestedTestConfigurationFile>();

        Assert.Equal( 0, configuration.Counter );

        var newEntries = this.Log.Entries.Skip( entriesBeforeRead )
            .Where( e => e.Severity is TestLoggerFactory.Severity.Warning or TestLoggerFactory.Severity.Error )
            .ToList();

        Assert.Empty( newEntries );
    }

    /// <summary>
    /// Writes a configuration file that carries a member unknown to the running version at every level of nesting,
    /// and returns the document that was written.
    /// </summary>
    /// <typeparam name="T">The type of the configuration file.</typeparam>
    /// <param name="configurationManager">The manager that gives the path of the file.</param>
    private JsonObject WriteFileWithUnknownMembers<T>( Configuration.ConfigurationManager configurationManager )
        where T : ConfigurationFile, new()
    {
        var document = UnknownMemberJson.CreateDocumentWithUnknownMembers( typeof(T), this._jsonOptions );
        Assert.NotNull( document );

        var path = configurationManager.GetFilePath<T>();
        this.FileSystem.WriteAllText( path, document.ToJsonString( this._jsonOptions ) );

        this.Logger.WriteLine( "Written to " + path + ":" );
        this.Logger.WriteLine( document.ToJsonString( this._jsonOptions ) );

        return document;
    }

    /// <summary>
    /// Reads the raw text of a configuration file and parses it, without going through the typed record.
    /// </summary>
    /// <typeparam name="T">The type of the configuration file.</typeparam>
    /// <param name="configurationManager">The manager that gives the path of the file.</param>
    private JsonObject ReadFile<T>( Configuration.ConfigurationManager configurationManager )
        where T : ConfigurationFile, new()
    {
        var text = this.FileSystem.ReadAllText( configurationManager.GetFilePath<T>() );
        this.Logger.WriteLine( "Read:" );
        this.Logger.WriteLine( text );

        return Assert.IsType<JsonObject>( JsonNode.Parse( text ) );
    }
}
