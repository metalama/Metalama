// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

/// <summary>
/// Tests of <see cref="SourceExpressionFactory"/> and of <see cref="SourceExpressionExtensions.GetSourceSyntax"/>.
/// </summary>
public sealed class SourceExpressionFactoryTests : UnitTestClass
{
    private static readonly SyntaxGenerationOptions _syntaxGenerationOptions = new( CodeFormattingOptions.Default );

    private const string _code = """
                                 enum Color { Red, Green }

                                 class C
                                 {
                                     int _number = 42;
                                     Color _color = Color.Green;
                                     int _sum = 1 + 2;

                                     void M( int p ) { }
                                 }
                                 """;

    [Fact]
    public void CreateInspectionOnly_TypeAndConstant()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var number = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_number" ), GetFieldType( compilation, "_number" ) );
        var color = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_color" ), GetFieldType( compilation, "_color" ) );
        var sum = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_sum" ), GetFieldType( compilation, "_sum" ) );

        Assert.Equal( SpecialType.Int32, number.Type.SpecialType );
        Assert.Equal( 42, number.AsTypedConstant!.Value.Value );
        Assert.Equal( 1, color.AsTypedConstant!.Value.Value );
        Assert.Null( sum.AsTypedConstant );
        Assert.Equal( "1 + 2", sum.AsString );
    }

    [Fact]
    public void CreateInspectionOnly_AsSyntaxNodeIsSourceNode()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var syntax = GetInitializer( compilation, "_number" );

        var expression = SourceExpressionFactory.CreateInspectionOnly( syntax, GetFieldType( compilation, "_number" ) );

        Assert.Same( syntax, expression.AsSyntaxNode );
        Assert.Same( syntax, expression.GetSourceSyntax() );
    }

    [Fact]
    public void CreateInspectionOnly_IsNotAssignable()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var expression = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_number" ), GetFieldType( compilation, "_number" ) );

        Assert.False( expression.IsAssignable );
    }

    /// <summary>
    /// Verifies that the expression cannot be emitted, neither through the syntax serialization of templates nor through its textual conversion.
    /// </summary>
    [Fact]
    public void CreateInspectionOnly_Emit_ReportsLAMA0297()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var expression = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_number" ), GetFieldType( compilation, "_number" ) );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var serializationContext = new SyntaxSerializationContext( compilation, _syntaxGenerationOptions );

            var serializationException =
                Assert.Throws<DiagnosticException>( () => ((IUserExpression) expression).ToTypedExpressionSyntax( serializationContext ) );

            Assert.Equal( "LAMA0297", serializationException.Diagnostics.Single().Id );

            var expressionHelper = new ExpressionHelper( compilation.CompilationContext.GetSyntaxGenerationContext( _syntaxGenerationOptions ) );

            var textException = Assert.Throws<DiagnosticException>( () => expressionHelper.ConvertExpressionToText( expression ) );

            Assert.Equal( "LAMA0297", textException.Diagnostics.Single().Id );
        }
    }

    [Fact]
    public void CreateInspectionOnly_ForeignSyntaxTree_Throws()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        Assert.Throws<ArgumentException>(
            () => SourceExpressionFactory.CreateInspectionOnly( SyntaxFactory.ParseExpression( "42" ), GetFieldType( compilation, "_number" ) ) );
    }

    [Fact]
    public void GetSourceSyntax_FieldInitializer_ReturnsSourceNode()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "_number" ).Single();

        Assert.Same( GetInitializer( compilation, "_number" ), field.InitializerExpression.AssertNotNull().GetSourceSyntax() );
    }

    [Fact]
    public void GetSourceSyntax_Parameter_ReturnsNull()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var parameter = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single().Parameters[0];

        Assert.Null( parameter.GetSourceSyntax() );
    }

    [Fact]
    public void GetSourceSyntax_TypedConstant_ReturnsNull()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        Assert.Null( TypedConstant.Create( 1, compilation.Factory.GetSpecialType( SpecialType.Int32 ) ).GetSourceSyntax() );
    }

    [Fact]
    public void GetSourceSyntax_BuiltExpression_ReturnsNull()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            Assert.Null( ExpressionFactory.Literal( 1 ).GetSourceSyntax() );
        }
    }

    private static ExpressionSyntax GetInitializer( CompilationModel compilation, string fieldName )
        => compilation.RoslynCompilation.SyntaxTrees
            .SelectMany( t => t.GetRoot().DescendantNodes() )
            .OfType<VariableDeclaratorSyntax>()
            .Single( v => v.Identifier.Text == fieldName )
            .Initializer!.Value;

    private static IType GetFieldType( CompilationModel compilation, string fieldName )
        => compilation.Types.OfName( "C" ).Single().Fields.OfName( fieldName ).Single().Type;
}
