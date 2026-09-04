// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace Metalama.Backstage.Testing;

/// <summary>
/// Builds the JSON documents that carry members which the running version does not declare, and asserts that those
/// members are still present after a round trip.
/// </summary>
/// <remarks>
/// The documents are derived from the serialization metadata of a type instead of being written by hand, so a test
/// based on this class covers the members and the nested types that are added to a configuration file later without
/// being updated. The traversal follows the metadata rather than the shape of the document, because a JSON object
/// stands either for an object, which accepts a member of any kind, or for a dictionary, whose values all have the
/// declared type.
/// </remarks>
[PublicAPI]
public static class UnknownMemberJson
{
    /// <summary>
    /// The prefix of the name of every member that this class adds. No version of Metalama declares a member with
    /// this prefix, which is what makes it a valid substitute for a member introduced by a later version.
    /// </summary>
    public const string UnknownMemberPrefix = "unknownMember";

    /// <summary>
    /// The key of the dictionary entry that <see cref="PopulateContainers"/> adds. It is a decimal integer, so that
    /// it is also a valid key of a dictionary whose key is a number.
    /// </summary>
    private const string _addedEntryKey = "1";

    /// <summary>
    /// Serializes a default instance of a type, adds an entry to every dictionary and every array whose values are
    /// objects, and then adds an unknown member of every JSON kind to every object of the document, at every level
    /// of nesting.
    /// </summary>
    /// <param name="type">The type whose serialization metadata drives the traversal.</param>
    /// <param name="options">The options that give access to the serialization metadata.</param>
    /// <returns>The document, or <c>null</c> if no default instance of <paramref name="type"/> can be created.</returns>
    public static JsonObject? CreateDocumentWithUnknownMembers( Type type, JsonSerializerOptions options )
    {
        if ( CreateDefaultNode( type, options ) is not JsonObject document )
        {
            return null;
        }

        PopulateContainers( document, type, options );
        AddUnknownMembers( document, type, options );

        return document;
    }

    /// <summary>
    /// Adds an entry to every dictionary and every array of the document whose values are objects, so that the
    /// traversal of <see cref="AddUnknownMembers"/> reaches the value of a dictionary and the element of an array.
    /// </summary>
    /// <param name="node">The document to amend in place.</param>
    /// <param name="type">The type that the document represents.</param>
    /// <param name="options">The options that give access to the serialization metadata.</param>
    public static void PopulateContainers( JsonNode? node, Type type, JsonSerializerOptions options )
    {
        var typeInfo = GetTypeInfo( type, options );
        var elementType = JsonExtensionDataVerifier.GetElementType( type );

        switch ( typeInfo?.Kind )
        {
            case JsonTypeInfoKind.Object when node is JsonObject jsonObject:
                foreach ( var property in GetDeclaredProperties( typeInfo ) )
                {
                    if ( !jsonObject.TryGetPropertyValue( property.Name, out var child ) )
                    {
                        continue;
                    }

                    // A nested object that is null by default is replaced by a default instance, so that the
                    // members of its type are covered as well.
                    if ( child == null && GetTypeInfo( property.PropertyType, options )?.Kind == JsonTypeInfoKind.Object )
                    {
                        child = CreateDefaultNode( property.PropertyType, options );

                        if ( child != null )
                        {
                            jsonObject[property.Name] = child;
                        }
                    }

                    PopulateContainers( child, property.PropertyType, options );
                }

                break;

            case JsonTypeInfoKind.Dictionary when node is JsonObject dictionary && elementType != null:
                if ( dictionary.Count == 0
                     && GetTypeInfo( elementType, options )?.Kind == JsonTypeInfoKind.Object
                     && CreateDefaultNode( elementType, options ) is { } addedValue )
                {
                    dictionary[_addedEntryKey] = addedValue;
                }

                foreach ( var entry in dictionary.ToList() )
                {
                    PopulateContainers( entry.Value, elementType, options );
                }

                break;

            case JsonTypeInfoKind.Enumerable when node is JsonArray array && elementType != null:
                if ( array.Count == 0
                     && GetTypeInfo( elementType, options )?.Kind == JsonTypeInfoKind.Object
                     && CreateDefaultNode( elementType, options ) is { } addedElement )
                {
                    array.Add( addedElement );
                }

                for ( var i = 0; i < array.Count; i++ )
                {
                    PopulateContainers( array[i], elementType, options );
                }

                break;
        }
    }

    /// <summary>
    /// Adds an unknown member of every JSON kind to every object of the document, at every level of nesting,
    /// including the values of a dictionary and the elements of an array.
    /// </summary>
    /// <param name="node">The document to amend in place.</param>
    /// <param name="type">The type that the document represents.</param>
    /// <param name="options">The options that give access to the serialization metadata.</param>
    /// <param name="path">The path of <paramref name="node"/>, used to give each added member a distinct value.</param>
    public static void AddUnknownMembers( JsonNode? node, Type type, JsonSerializerOptions options, string path = "$" )
    {
        var typeInfo = GetTypeInfo( type, options );
        var elementType = JsonExtensionDataVerifier.GetElementType( type );

        switch ( typeInfo?.Kind )
        {
            case JsonTypeInfoKind.Object when node is JsonObject jsonObject:
                // The nested nodes are visited first, so that the members added at this level are not visited in turn.
                foreach ( var property in GetDeclaredProperties( typeInfo ) )
                {
                    if ( jsonObject.TryGetPropertyValue( property.Name, out var child ) )
                    {
                        AddUnknownMembers( child, property.PropertyType, options, path + "." + property.Name );
                    }
                }

                AddUnknownMembersToObject( jsonObject, path );

                break;

            case JsonTypeInfoKind.Dictionary when node is JsonObject dictionary && elementType != null:
                foreach ( var entry in dictionary.ToList() )
                {
                    AddUnknownMembers( entry.Value, elementType, options, path + "." + entry.Key );
                }

                break;

            case JsonTypeInfoKind.Enumerable when node is JsonArray array && elementType != null:
                for ( var i = 0; i < array.Count; i++ )
                {
                    AddUnknownMembers( array[i], elementType, options, path + "[" + i + "]" );
                }

                break;
        }
    }

    /// <summary>
    /// Adds an unknown member of every JSON kind to a single object: a string, a number, a boolean, a null, an
    /// array and an object.
    /// </summary>
    /// <param name="jsonObject">The object to amend in place.</param>
    /// <param name="path">The path of <paramref name="jsonObject"/>, used to give each member a distinct value.</param>
    public static void AddUnknownMembersToObject( JsonObject jsonObject, string path )
    {
        jsonObject[UnknownMemberPrefix + "String"] = JsonValue.Create( "string at " + path );
        jsonObject[UnknownMemberPrefix + "Number"] = JsonValue.Create( 42.5 );
        jsonObject[UnknownMemberPrefix + "Boolean"] = JsonValue.Create( true );
        jsonObject[UnknownMemberPrefix + "Null"] = null;

        jsonObject[UnknownMemberPrefix + "Array"] = new JsonArray(
            JsonValue.Create( 1 ),
            JsonValue.Create( "element at " + path ),
            new JsonObject { [UnknownMemberPrefix + "InArrayElement"] = JsonValue.Create( "in array element at " + path ) } );

        jsonObject[UnknownMemberPrefix + "Object"] = new JsonObject
        {
            [UnknownMemberPrefix + "Nested"] = JsonValue.Create( "nested at " + path ),
            [UnknownMemberPrefix + "Deeper"] = new JsonObject { [UnknownMemberPrefix + "Deepest"] = JsonValue.Create( 7 ) }
        };
    }

    /// <summary>
    /// Asserts that every member of <paramref name="expected"/> whose name starts with
    /// <see cref="UnknownMemberPrefix"/> is present in <paramref name="actual"/>, at the same path and with the
    /// same value, and that the two documents carry the same number of them in total.
    /// </summary>
    /// <param name="expected">The document that was given to the deserializer.</param>
    /// <param name="actual">The document that the serializer produced from the deserialized instance.</param>
    public static void AssertUnknownMembersPreserved( JsonNode? expected, JsonNode? actual )
    {
        AssertUnknownMembersPreservedCore( expected, actual, "$" );

        // A member nested in a container that is dropped as a whole would escape the walk above, because the walk
        // stops where the two documents no longer have the same shape.
        Assert.Equal( CountUnknownMembers( expected ), CountUnknownMembers( actual ) );
    }

    /// <summary>
    /// Returns the number of members whose name starts with <see cref="UnknownMemberPrefix"/> in a document,
    /// counted at every level of nesting.
    /// </summary>
    /// <param name="node">The document to inspect.</param>
    public static int CountUnknownMembers( JsonNode? node )
        => node switch
        {
            JsonObject jsonObject => jsonObject.Sum(
                property => (property.Key.StartsWith( UnknownMemberPrefix, StringComparison.Ordinal ) ? 1 : 0)
                            + CountUnknownMembers( property.Value ) ),
            JsonArray jsonArray => jsonArray.Sum( CountUnknownMembers ),
            _ => 0
        };

    private static void AssertUnknownMembersPreservedCore( JsonNode? expected, JsonNode? actual, string path )
    {
        switch ( expected )
        {
            case JsonObject expectedObject:
                var actualObject = Assert.IsAssignableFrom<JsonObject>( actual );

                foreach ( var property in expectedObject )
                {
                    var childPath = path + "." + property.Key;

                    if ( property.Key.StartsWith( UnknownMemberPrefix, StringComparison.Ordinal ) )
                    {
                        Assert.True(
                            actualObject.ContainsKey( property.Key ),
                            "The member '" + childPath + "', which the running version does not declare, was removed." );

                        Assert.Equal( ToComparableString( property.Value ), ToComparableString( actualObject[property.Key] ) );
                    }
                    else if ( actualObject.TryGetPropertyValue( property.Key, out var actualChild ) )
                    {
                        AssertUnknownMembersPreservedCore( property.Value, actualChild, childPath );
                    }
                }

                break;

            case JsonArray expectedArray:
                var actualArray = Assert.IsAssignableFrom<JsonArray>( actual );

                for ( var i = 0; i < Math.Min( expectedArray.Count, actualArray.Count ); i++ )
                {
                    AssertUnknownMembersPreservedCore( expectedArray[i], actualArray[i], path + "[" + i + "]" );
                }

                break;
        }
    }

    private static JsonNode? CreateDefaultNode( Type type, JsonSerializerOptions options )
    {
        var instance = CreateInstance( type );

        return instance == null ? null : JsonSerializer.SerializeToNode( instance, type, options );
    }

    /// <summary>
    /// Creates an instance of a type with the parameterless constructor, or, when the type has none, with the
    /// constructor that has the fewest parameters, passing the default value of each parameter.
    /// </summary>
    /// <remarks>
    /// The values of the members do not matter, because only the shape of the document is used. A type such as
    /// <c>UserDiagnosticRegistration</c> has no parameterless constructor, and it would otherwise be left out of the
    /// round trip.
    /// </remarks>
    /// <param name="type">The type to instantiate.</param>
    private static object? CreateInstance( Type type )
    {
        try
        {
            return Activator.CreateInstance( type, nonPublic: true );
        }
        catch ( MissingMethodException )
        {
            var constructor = type.GetConstructors( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )
                .OrderBy( c => c.GetParameters().Length )
                .FirstOrDefault();

            if ( constructor == null )
            {
                return null;
            }

            var arguments = constructor.GetParameters()
                .Select( p => p.ParameterType.IsValueType ? Activator.CreateInstance( p.ParameterType ) : null )
                .ToArray();

            try
            {
                return constructor.Invoke( arguments );
            }
            catch ( TargetInvocationException )
            {
                return null;
            }
        }
    }

    private static JsonTypeInfo? GetTypeInfo( Type type, JsonSerializerOptions options )
        => options.TryGetTypeInfo( type, out var typeInfo ) ? typeInfo : null;

    private static IEnumerable<JsonPropertyInfo> GetDeclaredProperties( JsonTypeInfo typeInfo )
        => typeInfo.Properties.Where(
            p => p.AttributeProvider is not MemberInfo member || !member.IsDefined( typeof(JsonExtensionDataAttribute), true ) );

    private static string ToComparableString( JsonNode? node ) => node?.ToJsonString() ?? "null";
}
