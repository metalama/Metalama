// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class DisplayStringFormatterTests : UnitTestClass
{
    [Theory]
    [InlineData( "int" )]
    [InlineData( "int?" )]
    [InlineData( "string?" )]
    [InlineData( "decimal?" )]
    [InlineData( "(int, string)" )]
    [InlineData( "void" )]
    [InlineData( "Action<int>" )]
    public void Type( string type )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( $"using System; abstract class C {{ public abstract {type} M(); }}" );
        var typeInterface = compilation.Types.Single().Methods.Single().ReturnType;

        Assert.Equal( type, typeInterface.ToDisplayString() );
    }

    private const string _nullableCode = """
                                         using System;
                                         using System.Collections.Generic;

                                         interface IService { }

                                         class Container<T>
                                             where T : class
                                         {
                                             public IService? NullableReference = null;
                                             public IService NonNullableReference = null!;
                                             public List<IService?> NullableTypeArgument = null!;
                                             public List<IService> NonNullableTypeArgument = null!;
                                             public IService[]? NullableArray = null;
                                             public IService?[] ArrayOfNullableReference = null!;
                                             public T? NullableTypeParameter = null;
                                             public T NonNullableTypeParameter = null!;
                                             public List<T?> NullableTypeParameterArgument = null!;
                                             public List<T> NonNullableTypeParameterArgument = null!;
                                             public int? NullableValue;
                                             public int NonNullableValue;
                                             public Dictionary<string, IService?>? NestedNullable = null;

                                             public void Method<TMethod>( TMethod? nullable, TMethod nonNullable )
                                                 where TMethod : class { }
                                         }
                                         """;

    /// <summary>
    /// Verifies that the display string of a type carries its nullable annotation, so that a nullable type and its
    /// non-nullable counterpart do not render identically (issue #1812).
    /// </summary>
    /// <remarks>
    /// A type parameter reached at the root of the formatting is qualified by its containing declaration, hence the
    /// <c>Container&lt;T&gt;/</c> prefix. That form is the pre-existing rendering of a type parameter and issue #1812
    /// leaves it out of scope: what this test pins is that the annotation is appended to it.
    /// </remarks>
    [Theory]
    [InlineData( "NullableReference", "IService?" )]
    [InlineData( "NonNullableReference", "IService" )]
    [InlineData( "NullableTypeArgument", "List<IService?>" )]
    [InlineData( "NonNullableTypeArgument", "List<IService>" )]
    [InlineData( "NullableArray", "IService[]?" )]
    [InlineData( "ArrayOfNullableReference", "IService?[]" )]
    [InlineData( "NullableTypeParameter", "Container<T>/T?" )]
    [InlineData( "NonNullableTypeParameter", "Container<T>/T" )]
    [InlineData( "NullableTypeParameterArgument", "List<T?>" )]
    [InlineData( "NonNullableTypeParameterArgument", "List<T>" )]
    [InlineData( "NullableValue", "int?" )]
    [InlineData( "NonNullableValue", "int" )]
    [InlineData( "NestedNullable", "Dictionary<string, IService?>?" )]
    public void NullableAnnotationIsRendered( string fieldName, string expectedDisplayString )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _nullableCode );
        var field = compilation.Types.OfName( "Container" ).Single().Fields.OfName( fieldName ).Single();

        Assert.Equal( expectedDisplayString, field.Type.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that the annotation is also rendered for a type parameter of a method, whose containing declaration
    /// is the method and not a type (issue #1812).
    /// </summary>
    [Theory]
    [InlineData( 0, "Container<T>.Method<TMethod>(TMethod?, TMethod)/TMethod?" )]
    [InlineData( 1, "Container<T>.Method<TMethod>(TMethod?, TMethod)/TMethod" )]
    public void NullableAnnotationIsRenderedForMethodTypeParameter( int parameterIndex, string expectedDisplayString )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _nullableCode );
        var method = compilation.Types.OfName( "Container" ).Single().Methods.OfName( "Method" ).Single();

        Assert.Equal( expectedDisplayString, method.Parameters[parameterIndex].Type.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a nullable type and its non-nullable counterpart never render to the same string, whichever
    /// well-known <see cref="CodeDisplayFormat"/> is used (issue #1812).
    /// </summary>
    /// <remarks>
    /// The complaint of issue #1812 is about the information content of the display string and not about the
    /// qualification of names, so it applies to every format.
    /// </remarks>
    [Theory]
    [InlineData( nameof(CodeDisplayFormat.MinimallyQualified) )]
    [InlineData( nameof(CodeDisplayFormat.FullyQualified) )]
    [InlineData( nameof(CodeDisplayFormat.DiagnosticMessage) )]
    [InlineData( nameof(CodeDisplayFormat.ShortDiagnosticMessage) )]
    public void NullableAndNonNullableRenderDifferently( string formatName )
    {
        var format = formatName switch
        {
            nameof(CodeDisplayFormat.MinimallyQualified) => CodeDisplayFormat.MinimallyQualified,
            nameof(CodeDisplayFormat.FullyQualified) => CodeDisplayFormat.FullyQualified,
            nameof(CodeDisplayFormat.DiagnosticMessage) => CodeDisplayFormat.DiagnosticMessage,
            _ => CodeDisplayFormat.ShortDiagnosticMessage
        };

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _nullableCode );
        var container = compilation.Types.OfName( "Container" ).Single();

        var nullableType = container.Fields.OfName( "NullableReference" ).Single().Type;
        var nonNullableType = container.Fields.OfName( "NonNullableReference" ).Single().Type;

        Assert.True( nullableType.IsNullable );
        Assert.False( nonNullableType.IsNullable );

        var nullableDisplayString = nullableType.ToDisplayString( format );

        Assert.EndsWith( "?", nullableDisplayString, System.StringComparison.Ordinal );
        Assert.NotEqual( nonNullableType.ToDisplayString( format ), nullableDisplayString );
    }

    /// <summary>
    /// Verifies that a reference type in an oblivious nullable context, whose <see cref="IType.IsNullable"/> is
    /// unknown, renders without an annotation (issue #1812).
    /// </summary>
    [Fact]
    public void ObliviousReferenceTypeIsRenderedWithoutAnnotation()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel(
            """
            #nullable disable

            interface IService { }

            class Container
            {
                public IService ObliviousReference = null;
            }
            """ );

        var type = compilation.Types.OfName( "Container" ).Single().Fields.OfName( "ObliviousReference" ).Single().Type;

        Assert.Null( type.IsNullable );
        Assert.Equal( "IService", type.ToDisplayString() );
    }
}
