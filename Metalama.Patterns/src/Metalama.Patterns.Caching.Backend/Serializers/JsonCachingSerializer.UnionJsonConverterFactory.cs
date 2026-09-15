// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Formatters.Utilities;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Patterns.Caching.Serializers
{
    public partial class JsonCachingSerializer
    {
        /// <summary>
        /// Creates the converter of a union of C# 15.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The factory accepts a type that implements <c>System.Runtime.CompilerServices.IUnion</c> and that declares
        /// at least one public constructor taking exactly one parameter. The second condition is what the converter
        /// needs in order to rebuild a union from the value of its case. A union declared with the <c>union</c>
        /// keyword always satisfies it, because the compiler synthesizes one such constructor per case. A class or a
        /// struct that is a union because it carries the union attribute satisfies it only when it creates its cases
        /// through constructors, and a type that creates them in another way keeps the conversion that
        /// <see cref="JsonSerializer"/> applies to any other type.
        /// </para>
        /// </remarks>
        private sealed class UnionJsonConverterFactory : JsonConverterFactory
        {
            private readonly JsonCachingSerializer _serializer;

            public UnionJsonConverterFactory( JsonCachingSerializer serializer )
            {
                this._serializer = serializer;
            }

            public override bool CanConvert( Type typeToConvert )
                => UnionReflection.IsUnion( typeToConvert ) && HasSingleParameterConstructor( typeToConvert );

            public override JsonConverter CreateConverter( Type typeToConvert, JsonSerializerOptions options )
                => (JsonConverter) Activator.CreateInstance(
                    typeof(UnionJsonConverter<>).MakeGenericType( typeToConvert ),
                    this._serializer )!;

            private static bool HasSingleParameterConstructor( Type type )
            {
                foreach ( var constructor in type.GetConstructors( BindingFlags.Public | BindingFlags.Instance ) )
                {
                    if ( constructor.GetParameters().Length == 1 )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
