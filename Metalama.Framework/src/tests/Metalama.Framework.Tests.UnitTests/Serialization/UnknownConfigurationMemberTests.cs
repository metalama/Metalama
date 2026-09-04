// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Metalama.Framework.ConfigurationFiles;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Serialization;

/// <summary>
/// Tests that a type serialized into a configuration file of <see cref="FrameworkConfigurationJsonContext"/> keeps
/// the JSON members that the running version does not declare. Several versions of Metalama share the same
/// configuration files, so a version that removed the members it does not know would destroy the content written by
/// a later version (#1923).
/// </summary>
public sealed class UnknownConfigurationMemberTests
{
    private readonly ITestOutputHelper _output;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true, PropertyNameCaseInsensitive = true, TypeInfoResolver = FrameworkConfigurationJsonContext.Default
    };

    public UnknownConfigurationMemberTests( ITestOutputHelper output )
    {
        this._output = output;
    }

    [Fact]
    public void EveryTypeOfFrameworkConfigurationJsonContextCarriesExtensionData()
    {
        var allTypes = JsonExtensionDataVerifier.GetSerializedObjectTypes( typeof(FrameworkConfigurationJsonContext) );

        foreach ( var type in allTypes )
        {
            this._output.WriteLine( type.FullName ?? type.Name );
        }

        // The traversal itself must find something, otherwise the test would pass for the wrong reason.
        Assert.NotEmpty( allTypes );

        var typesWithoutExtensionData = JsonExtensionDataVerifier.GetTypesWithoutExtensionData( typeof(FrameworkConfigurationJsonContext) );

        Assert.True(
            typesWithoutExtensionData.Count == 0,
            "The following types are serialized into a configuration file and declare no member annotated with "
            + "JsonExtensionDataAttribute, so a version that does not know a member of the file removes it:"
            + Environment.NewLine
            + string.Join( Environment.NewLine, typesWithoutExtensionData.SelectAsArray( t => "  " + t.FullName ) ) );
    }

    [Fact]
    public void UnknownMembersOfEveryTypeSurviveRoundTrip()
    {
        var testedTypes = 0;

        foreach ( var type in JsonExtensionDataVerifier.GetSerializedObjectTypes( typeof(FrameworkConfigurationJsonContext) ) )
        {
            if ( type.IsAbstract )
            {
                continue;
            }

            var document = UnknownMemberJson.CreateDocumentWithUnknownMembers( type, this._jsonOptions );

            if ( document == null )
            {
                // No instance of the type could be created, or the serialization of the instance is not a JSON object, so
                // no document can be derived from the type. Such a type is covered by the guard test only.
                this._output.WriteLine( "Skipped " + type.FullName + ": no document could be derived from the type." );

                continue;
            }

            this._output.WriteLine( type.FullName + ":" );
            this._output.WriteLine( document.ToJsonString( this._jsonOptions ) );

            var deserialized = JsonSerializer.Deserialize( document.ToJsonString(), type, this._jsonOptions );
            Assert.NotNull( deserialized );

            var roundTripped = JsonNode.Parse( JsonSerializer.Serialize( deserialized, type, this._jsonOptions ) );

            UnknownMemberJson.AssertUnknownMembersPreserved( document, roundTripped );
            Assert.True( UnknownMemberJson.CountUnknownMembers( document ) > 0 );

            testedTypes++;
        }

        Assert.True( testedTypes > 0, "No type could be tested." );
    }
}
