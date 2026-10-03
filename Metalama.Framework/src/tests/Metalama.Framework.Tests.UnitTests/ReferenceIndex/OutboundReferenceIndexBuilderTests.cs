// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

/// <summary>
/// Tests of <see cref="OutboundReferenceIndexBuilder"/>, which gives the outbound references of a declaration to introspection.
/// </summary>
/// <remarks>
/// The builder walks the source of a declaration as a declaration root. For a field, the root is the variable declarator, and for an
/// expression-bodied property, the root is the expression body, so the references in a field initializer and in an expression body are
/// outbound references of the field and of the property.
/// </remarks>
public sealed class OutboundReferenceIndexBuilderTests : UnitTestClass
{
    private const string _code = """
                                 class A
                                 {
                                     public static int F() => 0;

                                     public static int G() => 0;
                                 }

                                 class B
                                 {
                                     int _field = A.F();

                                     int Property => A.G();
                                 }
                                 """;

    [Theory]
    [InlineData( "_field", "F" )]
    [InlineData( "Property", "G" )]
    public void InitializerAndExpressionBody_HaveOutboundReferences( string memberName, string referencedMethodName )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var member = compilation.Types.OfName( "B" ).Single().FieldsAndProperties.OfName( memberName ).Single();

        var builder = new OutboundReferenceIndexBuilder( testContext.ServiceProvider );
        builder.IndexDeclaration( member, Xunit.TestContext.Current.CancellationToken );

        var invocations = builder.GetReferences()
            .Where( r => r.ReferenceKind == ReferenceKinds.Invocation )
            .Select( r => r.ReferencedSymbol.Name )
            .ToList();

        Assert.Equal( [referencedMethodName], invocations );
    }
}
