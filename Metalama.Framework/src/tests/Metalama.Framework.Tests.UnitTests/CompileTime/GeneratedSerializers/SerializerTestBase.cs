// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Serialization;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime.GeneratedSerializers
{
    public class SerializerTestBase : UnitTestClass
    {
        private protected static CompileTimeProject CreateCompileTimeProject( CompileTimeDomain domain, TestContext testContext, string code )
        {
            var runtimeCompilation = testContext.CreateCSharpCompilation(
                code,
                assemblyName: "test_A" );

            var compileTimeCompilationBuilder = new CompileTimeCompilationBuilder( testContext.ServiceProvider, domain );

            DiagnosticBag diagnosticBag = new();

            Assert.True(
                compileTimeCompilationBuilder.TryGetCompileTimeProject(
                    runtimeCompilation,
                    null,
                    Array.Empty<CompileTimeProject>(),
                    diagnosticBag,
                    false,
                    out var project,
                    CancellationToken.None ),
                string.Join( "\n", diagnosticBag.SelectAsReadOnlyCollection( x => x.ToString() ) ) );

            return project!;
        }

        private static Type GetLamaSerializerType( Type type )
        {
            var lamaSerializerTypes =
                type.GetNestedTypes()
                    .Where( nestedType => typeof(ISerializer).IsAssignableFrom( nestedType ) )
                    .ToArray();

            Assert.Single( lamaSerializerTypes );

            if ( lamaSerializerTypes[0].IsGenericTypeDefinition )
            {
                return lamaSerializerTypes[0].MakeGenericType( type.GenericTypeArguments );
            }
            else
            {
                return lamaSerializerTypes[0];
            }
        }

        private protected static ISerializer GetSerializer( Type type )
        {
            var lamaSerializerType = GetLamaSerializerType( type );

            return (ISerializer) Activator.CreateInstance( lamaSerializerType ).AssertNotNull();
        }

        private protected sealed class TestArgumentsReader : IArgumentsReader
        {
            private (string Name, object? Value, string? Scope)[]? _data;

            public bool TryGetValue<T>( string name, [NotNullWhen( true )] out T value, string? scope = null )
            {
                var dataValue =
                    this._data.AssertNotNull()
                        .SelectAsImmutableArray( x => ((string Name, object? Value, string? Scope)?) x )
                        .SingleOrDefault(
                            d => StringComparer.Ordinal.Equals( d.AssertNotNull().Name, name )
                                 && StringComparer.Ordinal.Equals( d.AssertNotNull().Scope, scope ) );

                if ( dataValue == null )
                {
                    value = default!;

                    return false;
                }

                value = (T) dataValue.Value.Value!;

                return true;
            }

            public T GetValue<T>( string name, string? scope = null )
            {
                if ( !this.TryGetValue<T>( name, out var value, scope ) )
                {
                    throw new InvalidOperationException();
                }

                return value;
            }

            public void SetData( params (string Name, object? Value, string? Scope)[] data )
            {
                this._data = data;
            }
        }

        private protected sealed class TestArgumentsWriter : IArgumentsWriter
        {
            private List<(string Name, object? Value, string? Scope)> Data { get; } = new();

            public void SetValue( string name, object? value, string? scope = null )
            {
                this.Data.Add( (name, value, scope) );
            }

            public TestArgumentsReader ToReader()
            {
                var reader = new TestArgumentsReader();
                reader.SetData( this.Data.ToArray() );

                return reader;
            }
        }
    }
}