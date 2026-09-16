// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters.Utilities;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Patterns.Caching.Serializers
{
    public partial class JsonCachingSerializer
    {
        /// <summary>
        /// Converts a union of C# 15 to and from JSON, by the type and the value of the case that it carries.
        /// </summary>
        /// <typeparam name="TUnion">The union type.</typeparam>
        /// <remarks>
        /// <para>
        /// The JSON form is an object with the property <c>caseType</c>, which holds the type name produced by
        /// <see cref="GetTypeName"/>, followed by the property <c>value</c>, which holds the value of the case. The
        /// reader requires <c>caseType</c> to come first, because the type of the case is what decides how
        /// <c>value</c> is read.
        /// </para>
        /// <para>
        /// A union whose <c>Value</c> property is <c>null</c> is written with a null <c>caseType</c> and no
        /// <c>value</c>, and is read back as the default value of the union. The value of the case does not identify
        /// the case when it is <c>null</c>, so the case itself is not preserved. The default value of a union has a
        /// null <c>Value</c> as well, so the two are indistinguishable through the interface that the compiler
        /// provides.
        /// </para>
        /// <para>
        /// The case is identified by the run-time type of its value, which two cases can share. A union that declares
        /// both <c>object</c> and <c>int?</c> reports <c>int</c> for a value of either case, and the round trip then
        /// yields the more specific of the two, which is <c>int?</c>. The <c>Value</c> property carries no case
        /// identifier, so no reading of it can do better.
        /// </para>
        /// </remarks>
        private sealed class UnionJsonConverter<TUnion> : JsonConverter<TUnion>
        {
            private const string _caseTypePropertyName = "caseType";
            private const string _valuePropertyName = "value";

            private static readonly ConcurrentDictionary<Type, ConstructorInfo> _caseConstructors = new();

            private readonly JsonCachingSerializer _serializer;
            private readonly Func<object, object?> _getCaseValue;

            public UnionJsonConverter( JsonCachingSerializer serializer )
            {
                this._serializer = serializer;

                if ( !UnionHelper.TryGetValuePropertyGetter( typeof(TUnion), out var getValue ) )
                {
                    throw new InvalidCacheItemException(
                        $"The type '{typeof(TUnion)}' is not a union, so it cannot be converted by the union converter." );
                }

                this._getCaseValue = getValue;
            }

            public override void Write( Utf8JsonWriter writer, TUnion value, JsonSerializerOptions options )
            {
                var caseValue = value is null ? null : this._getCaseValue( value );

                writer.WriteStartObject();

                if ( caseValue == null )
                {
                    writer.WriteNull( _caseTypePropertyName );
                }
                else
                {
                    var caseType = caseValue.GetType();
                    writer.WriteString( _caseTypePropertyName, this._serializer.GetTypeName( caseType ) );
                    writer.WritePropertyName( _valuePropertyName );
                    JsonSerializer.Serialize( writer, caseValue, caseType, options );
                }

                writer.WriteEndObject();
            }

            public override TUnion? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
            {
                if ( reader.TokenType == JsonTokenType.Null )
                {
                    return default;
                }

                if ( reader.TokenType != JsonTokenType.StartObject )
                {
                    throw new JsonException( $"An object was expected at the start of the JSON form of the union '{typeof(TUnion)}'." );
                }

                Type? caseType = null;
                object? caseValue = null;

                while ( reader.Read() )
                {
                    switch ( reader.TokenType )
                    {
                        case JsonTokenType.EndObject:
                            return caseType == null ? default : (TUnion) CreateUnion( caseType, caseValue );

                        case JsonTokenType.PropertyName:
                            var propertyName = reader.GetString();
                            reader.Read();

                            switch ( propertyName )
                            {
                                case _caseTypePropertyName:
                                    caseType = reader.TokenType == JsonTokenType.Null
                                        ? null
                                        : this._serializer.ResolveTypeName( reader.GetString()! );

                                    break;

                                case _valuePropertyName:
                                    if ( caseType == null )
                                    {
                                        throw new JsonException(
                                            $"The '{_valuePropertyName}' property of the JSON form of the union '{typeof(TUnion)}' comes before the '{_caseTypePropertyName}' property." );
                                    }

                                    caseValue = JsonSerializer.Deserialize( ref reader, caseType, options );

                                    break;

                                default:
                                    reader.Skip();

                                    break;
                            }

                            break;
                    }
                }

                throw new JsonException( $"The JSON form of the union '{typeof(TUnion)}' ends before its closing brace." );
            }

            /// <summary>
            /// Creates a value of the union by invoking the constructor that takes the case type.
            /// </summary>
            private static object CreateUnion( Type caseType, object? caseValue )
                => _caseConstructors.GetOrAdd( caseType, GetCaseConstructor ).Invoke( new[] { caseValue } );

            /// <remarks>
            /// <para>
            /// The constructor is searched in three steps, from the most specific match to the least specific one, so
            /// that the result does not depend on the order in which <see cref="Type.GetConstructors()"/> returns the
            /// constructors, which the runtime does not define.
            /// </para>
            /// <para>
            /// The second step exists because boxing a <see cref="Nullable{T}"/> that has a value produces a boxed
            /// value of the underlying type. A case declared <c>int?</c> therefore reports <c>int</c> as the type of
            /// its value, and the constructor that takes it has a parameter of type <c>int?</c>.
            /// </para>
            /// </remarks>
            private static ConstructorInfo GetCaseConstructor( Type caseType )
            {
                ConstructorInfo? nullableConstructor = null;
                ConstructorInfo? assignableConstructor = null;

                foreach ( var constructor in typeof(TUnion).GetConstructors( BindingFlags.Public | BindingFlags.Instance ) )
                {
                    var parameters = constructor.GetParameters();

                    if ( parameters.Length != 1 )
                    {
                        continue;
                    }

                    var parameterType = parameters[0].ParameterType;

                    if ( parameterType == caseType )
                    {
                        return constructor;
                    }

                    if ( nullableConstructor == null && Nullable.GetUnderlyingType( parameterType ) == caseType )
                    {
                        nullableConstructor = constructor;
                    }

                    // A case type may be a base type or an interface of the type of the value, in which case there is
                    // no more specific match.
                    if ( assignableConstructor == null && parameterType.IsAssignableFrom( caseType ) )
                    {
                        assignableConstructor = constructor;
                    }
                }

                return nullableConstructor
                       ?? assignableConstructor
                       ?? throw new InvalidCacheItemException(
                           $"The union '{typeof(TUnion)}' declares no constructor that takes a value of type '{caseType}'." );
            }
        }
    }
}
