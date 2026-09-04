// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Testing;

/// <summary>
/// Enumerates the types that a <see cref="JsonSerializerContext"/> serializes, and reports those that do not
/// preserve the JSON members which the running version does not declare.
/// </summary>
/// <remarks>
/// Several versions of Metalama share the same configuration files. A configuration file is rewritten from a typed
/// record, so a version removes every member that it does not declare, unless the type carries a member annotated
/// with <see cref="JsonExtensionDataAttribute"/>. A test based on this class fails as soon as a configuration type
/// is added without that member.
/// </remarks>
[PublicAPI]
public static class JsonExtensionDataVerifier
{
    private static readonly ImmutableHashSet<Type> _leafTypes = ImmutableHashSet.Create(
        typeof(string),
        typeof(object),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri),
        typeof(Version),
        typeof(JsonElement),
        typeof(JsonDocument),
        typeof(JsonNode),
        typeof(JsonObject),
        typeof(JsonArray),
        typeof(JsonValue) );

    /// <summary>
    /// Returns the object types reachable from the <see cref="JsonSerializableAttribute"/> declarations of a
    /// <see cref="JsonSerializerContext"/>, ordered by full name. Primitives, enumerations and the types listed as
    /// leaves are excluded, and a collection or a dictionary is replaced by the type of its elements or of its
    /// values.
    /// </summary>
    /// <param name="jsonSerializerContextType">The type of the <see cref="JsonSerializerContext"/> to inspect.</param>
    public static IReadOnlyList<Type> GetSerializedObjectTypes( Type jsonSerializerContextType )
    {
        var objectTypes = new SortedDictionary<string, Type>( StringComparer.Ordinal );
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>();

        // JsonSerializableAttribute does not expose the type as a property, so it is read from the argument of the constructor.
        foreach ( var attribute in jsonSerializerContextType.GetCustomAttributesData() )
        {
            if ( attribute.AttributeType == typeof(JsonSerializableAttribute) && attribute.ConstructorArguments.Count == 1
                                                                             && attribute.ConstructorArguments[0].Value is Type serializableType )
            {
                queue.Enqueue( serializableType );
            }
        }

        while ( queue.Count > 0 )
        {
            var type = Unwrap( queue.Dequeue() );

            if ( !visited.Add( type ) || IsLeaf( type ) )
            {
                continue;
            }

            objectTypes[type.FullName ?? type.Name] = type;

            foreach ( var property in GetSerializedProperties( type ) )
            {
                queue.Enqueue( property.PropertyType );
            }
        }

        return objectTypes.Values.ToImmutableArray();
    }

    /// <summary>
    /// Returns the types of <see cref="GetSerializedObjectTypes"/> that declare or inherit no member annotated with
    /// <see cref="JsonExtensionDataAttribute"/>. An empty result means that the context preserves every unknown
    /// member at every level of nesting.
    /// </summary>
    /// <param name="jsonSerializerContextType">The type of the <see cref="JsonSerializerContext"/> to inspect.</param>
    public static IReadOnlyList<Type> GetTypesWithoutExtensionData( Type jsonSerializerContextType )
        => GetSerializedObjectTypes( jsonSerializerContextType )
            .Where( t => !HasExtensionData( t ) )
            .ToImmutableArray();

    /// <summary>
    /// Determines whether a type declares or inherits a property or a field annotated with
    /// <see cref="JsonExtensionDataAttribute"/>.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    public static bool HasExtensionData( Type type )
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        return type.GetProperties( bindingFlags ).Any( m => m.IsDefined( typeof(JsonExtensionDataAttribute), true ) )
               || type.GetFields( bindingFlags ).Any( m => m.IsDefined( typeof(JsonExtensionDataAttribute), true ) );
    }

    /// <summary>
    /// Returns the properties of a type that take part in its JSON representation, excluding the member that
    /// carries the extension data.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    public static IEnumerable<PropertyInfo> GetSerializedProperties( Type type )
        => type.GetProperties( BindingFlags.Public | BindingFlags.Instance )
            .Where(
                p => p.GetIndexParameters().Length == 0
                     && p.GetCustomAttribute<JsonExtensionDataAttribute>() == null
                     && p.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always );

    /// <summary>
    /// Returns the type of the values of a dictionary or of the elements of a collection, or <c>null</c> if the
    /// given type is neither.
    /// </summary>
    /// <remarks>
    /// The property <c>JsonTypeInfo.ElementType</c> gives the same information, but it requires
    /// <c>System.Text.Json</c> 9, and the projects that are built against the older Roslyn use version 8.
    /// </remarks>
    /// <param name="type">The type to inspect.</param>
    public static Type? GetElementType( Type type )
    {
        if ( _leafTypes.Contains( type ) )
        {
            return null;
        }

        if ( type.IsArray )
        {
            return type.GetElementType();
        }

        return GetGenericArgument( type, typeof(IDictionary<,>), 1 )
               ?? GetGenericArgument( type, typeof(IReadOnlyDictionary<,>), 1 )
               ?? GetGenericArgument( type, typeof(IEnumerable<>), 0 );
    }

    private static Type Unwrap( Type type )
    {
        // A collection or a dictionary carries no extension data of its own. The member that a version does not
        // know is carried by the element of the array or by the value of the dictionary, so the traversal replaces
        // the container by what it contains.
        while ( true )
        {
            if ( _leafTypes.Contains( type ) )
            {
                return type;
            }

            if ( Nullable.GetUnderlyingType( type ) is { } underlyingType )
            {
                type = underlyingType;

                continue;
            }

            if ( GetElementType( type ) is { } elementType )
            {
                type = elementType;

                continue;
            }

            return type;
        }
    }

    private static Type? GetGenericArgument( Type type, Type openGenericInterface, int index )
    {
        var candidates = type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == openGenericInterface
            ? new[] { type }
            : type.GetInterfaces().Where( i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface ).ToArray();

        return candidates.Length == 0 ? null : candidates[0].GetGenericArguments()[index];
    }

    private static bool IsLeaf( Type type ) => type.IsPrimitive || type.IsEnum || _leafTypes.Contains( type );
}
