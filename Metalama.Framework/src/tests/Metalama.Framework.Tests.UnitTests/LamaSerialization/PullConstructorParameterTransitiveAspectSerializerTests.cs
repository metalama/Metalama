// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Serialization;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

/// <summary>
/// Tests the deserialization of <c>PullConstructorParameterTransitiveAspect</c> from a payload that a producer built
/// with an earlier version of Metalama would have written.
/// </summary>
/// <remarks>
/// <para>
/// A transitive aspect is serialized by the producer's engine and deserialized by the consumer's, and the two are not
/// required to be the same version. <c>CompileTimeProjectRepository.Builder</c> refuses a reference only when its
/// <c>ManifestVersion</c> differs from the current one, which has not changed, and otherwise merely reports the mixed
/// versions as a warning, so a payload written by an earlier engine does reach this serializer.
/// </para>
/// <para>
/// Pull request #1784 adds five constructor arguments to the payload and reads them with <c>AssertNotNull</c>. They
/// describe a parameter that the aspect can also obtain from the reference it already carried, so they are a fallback
/// rather than essential data, and their absence should degrade to the previous behaviour instead of failing an
/// assertion. See the review of that pull request.
/// </para>
/// </remarks>
public sealed class PullConstructorParameterTransitiveAspectSerializerTests : UnitTestClass
{
    public PullConstructorParameterTransitiveAspectSerializerTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _code = """
                                 public class C
                                 {
                                     public C( string s ) { }
                                 }
                                 """;

    /// <summary>
    /// Verifies that a payload carrying only the arguments that existed before pull request #1784 deserializes without
    /// throwing.
    /// </summary>
    [Fact]
    public void PayloadWithoutTheParameterDescriptionDeserializes()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var parameter = compilation.Types.OfName( "C" ).Single().Constructors.Single().Parameters[0];

        var arguments = new TestArgumentsReader
        {
            // The four arguments that the serializer wrote before pull request #1784. The two strategies are optional
            // and were already read without an assertion.
            { "_parameter", parameter.ToRef() },
            { "_order", 0 }
        };

        var exception = Record.Exception( () => CreateInstance( arguments ) );

        Assert.Null( exception );
    }

    /// <summary>
    /// Verifies that a payload carrying the arguments added by pull request #1784 deserializes, which distinguishes a
    /// failure of the test harness from the compatibility failure the previous test reports.
    /// </summary>
    [Fact]
    public void PayloadWithTheParameterDescriptionDeserializes()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var constructor = compilation.Types.OfName( "C" ).Single().Constructors.Single();
        var parameter = constructor.Parameters[0];

        var arguments = new TestArgumentsReader
        {
            { "_parameter", parameter.ToRef() },
            { "_order", 0 },
            { "_declaringConstructor", constructor.ToRef() },
            { "_parameterName", parameter.Name },
            { "_parameterType", parameter.Type.ToRef() },
            { "_parameterIndex", parameter.Index },
            { "_parameterRefKind", parameter.RefKind }
        };

        var exception = Record.Exception( () => CreateInstance( arguments ) );

        Assert.Null( exception );
    }

    /// <summary>
    /// Invokes the serializer of <c>PullConstructorParameterTransitiveAspect</c>, which is a private nested type.
    /// </summary>
    /// <remarks>
    /// It is reached by reflection, as the serialization infrastructure itself does, so that the test does not require
    /// the accessibility of a production type to be widened.
    /// </remarks>
    private static object CreateInstance( IArgumentsReader arguments )
    {
        var serializerType = typeof(PullConstructorParameterTransitiveAspect).GetNestedType( "Serializer", BindingFlags.NonPublic );

        Assert.NotNull( serializerType );

        var serializer = Activator.CreateInstance( serializerType, nonPublic: true );

        Assert.NotNull( serializer );

        var method = serializerType.GetMethod( "CreateInstance", [typeof(IArgumentsReader)] );

        Assert.NotNull( method );

        try
        {
            var instance = method.Invoke( serializer, [arguments] );

            Assert.NotNull( instance );

            return instance;
        }
        catch ( TargetInvocationException e ) when ( e.InnerException != null )
        {
            // The exception of the serializer is what the test is about, so the reflection wrapper is removed.
            throw e.InnerException;
        }
    }

    /// <summary>
    /// An <see cref="IArgumentsReader"/> over a dictionary, which reports an absent argument the way the serialization
    /// reader does, that is, as the default value of the requested type.
    /// </summary>
    private sealed class TestArgumentsReader : IArgumentsReader, IEnumerable<KeyValuePair<string, object?>>
    {
        private readonly Dictionary<string, object?> _values = new( StringComparer.Ordinal );

        public void Add( string name, object? value ) => this._values.Add( name, value );

        public bool TryGetValue<T>( string name, [MaybeNullWhen( false )] out T value, string? scope = null )
        {
            if ( this._values.TryGetValue( name, out var untyped ) && untyped is T typed )
            {
                value = typed;

                return true;
            }

            value = default;

            return false;
        }

        public T? GetValue<T>( string name, string? scope = null )
            => this._values.TryGetValue( name, out var untyped ) && untyped is T typed ? typed : default;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this._values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
