// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Serialization;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Serialization;

/// <summary>
/// Tests that a type serialized into a configuration file keeps the JSON members that the running version does not
/// declare. Several versions of Metalama share the same configuration files, so a version that removed the members
/// it does not know would destroy the content written by a later version (#1923).
/// </summary>
public sealed class UnknownConfigurationMemberTests : JsonSerializationTestsBase
{
    public UnknownConfigurationMemberTests( ITestOutputHelper output ) : base( output ) { }

    [Fact]
    public void EveryTypeOfBackstageJsonContextCarriesExtensionData()
    {
        var allTypes = JsonExtensionDataVerifier.GetSerializedObjectTypes( typeof(BackstageJsonContext) );

        foreach ( var type in allTypes )
        {
            this.Output.WriteLine( type.FullName ?? type.Name );
        }

        // The traversal itself must find something, otherwise the test would pass for the wrong reason.
        Assert.NotEmpty( allTypes );

        var typesWithoutExtensionData = JsonExtensionDataVerifier.GetTypesWithoutExtensionData( typeof(BackstageJsonContext) );

        Assert.True(
            typesWithoutExtensionData.Count == 0,
            "The following types are serialized into a configuration file and declare no member annotated with "
            + "JsonExtensionDataAttribute, so a version that does not know a member of the file removes it:"
            + Environment.NewLine
            + string.Join( Environment.NewLine, typesWithoutExtensionData.Select( t => "  " + t.FullName ) ) );
    }

    [Fact]
    public void UnknownMembersOfEveryTypeSurviveRoundTrip()
    {
        var testedTypes = 0;

        foreach ( var type in JsonExtensionDataVerifier.GetSerializedObjectTypes( typeof(BackstageJsonContext) ) )
        {
            if ( type.IsAbstract )
            {
                continue;
            }

            var document = UnknownMemberJson.CreateDocumentWithUnknownMembers( type, this.JsonOptions );

            if ( document == null )
            {
                // The type has no parameterless constructor, so no default instance can be built to derive the
                // document from. Such a type is covered by the guard test only.
                this.Output.WriteLine( "Skipped " + type.FullName + ": no default instance." );

                continue;
            }

            this.Output.WriteLine( type.FullName + ":" );
            this.Output.WriteLine( document.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ) );

            var deserialized = JsonSerializer.Deserialize( document.ToJsonString(), type, this.JsonOptions );
            Assert.NotNull( deserialized );

            var roundTripped = JsonNode.Parse( this.JsonService.Serialize( deserialized, type ) );

            UnknownMemberJson.AssertUnknownMembersPreserved( document, roundTripped );
            Assert.True( UnknownMemberJson.CountUnknownMembers( document ) > 0 );

            testedTypes++;
        }

        Assert.True( testedTypes > 0, "No type could be tested." );
    }

    [Fact]
    public void UnknownMembersOfEveryJsonKindSurviveRoundTrip()
    {
        // The members below stand for the members that a later version of Metalama writes into telemetry.json. They
        // cover the six kinds of JSON value, at the root and at two levels of nesting.
        const string json = """
                            {
                              "ExceptionReportingAction": 1,
                              "unknownMemberString": "a string",
                              "unknownMemberNumber": 42.5,
                              "unknownMemberBoolean": true,
                              "unknownMemberNull": null,
                              "unknownMemberArray": [
                                1,
                                "two",
                                {
                                  "unknownMemberInArrayElement": true
                                }
                              ],
                              "unknownMemberObject": {
                                "unknownMemberNested": "nested",
                                "unknownMemberDeeper": {
                                  "unknownMemberDeepest": 7
                                }
                              }
                            }
                            """;

        var document = JsonNode.Parse( json )!;

        var deserialized = JsonSerializer.Deserialize<TelemetryConfiguration>( json, this.JsonOptions );
        Assert.NotNull( deserialized );

        var roundTripped = JsonNode.Parse( this.JsonService.Serialize( deserialized, typeof(TelemetryConfiguration) ) );
        this.Output.WriteLine( roundTripped!.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ) );

        UnknownMemberJson.AssertUnknownMembersPreserved( document, roundTripped );

        // The known member must still be read.
        Assert.Equal( TelemetryConsent.Yes, deserialized.ExceptionConsent );
    }

    [Fact]
    public void UnknownMembersOfDictionaryValueSurviveRoundTrip()
    {
        // 'notifications' is a dictionary whose values are objects. The extension data is carried by the value
        // itself, because a dictionary has no place to store it.
        const string json = """
                            {
                              "notifications": {
                                "notification1": {
                                  "disabled": true,
                                  "unknownMemberInDictionaryValue": "a string"
                                }
                              },
                              "unknownMemberAtRoot": 1
                            }
                            """;

        var document = JsonNode.Parse( json )!;

        var deserialized = JsonSerializer.Deserialize<ToastNotificationsConfiguration>( json, this.JsonOptions );
        Assert.NotNull( deserialized );

        var roundTripped = JsonNode.Parse( this.JsonService.Serialize( deserialized, typeof(ToastNotificationsConfiguration) ) );
        this.Output.WriteLine( roundTripped!.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ) );

        UnknownMemberJson.AssertUnknownMembersPreserved( document, roundTripped );

        Assert.True( deserialized.Notifications["notification1"].Disabled );
    }
}
