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
    /// <summary>
    /// The options of syntax generation that the tests use to serialize an expression.
    /// </summary>
    private static readonly SyntaxGenerationOptions _syntaxGenerationOptions = new( CodeFormattingOptions.Default );

    /// <summary>
    /// The code of most tests: fields whose initializers are a constant, an enumeration member, a sum, <c>null</c> and <c>default</c>, a property and
    /// an event with initializers, and a method with a parameter.
    /// </summary>
    private const string _code = """
                                 enum Color { Red, Green }

                                 class C
                                 {
                                     int _number = 42;
                                     Color _color = Color.Green;
                                     int _sum = 1 + 2;
                                     object? _null = null;
                                     int _default = default;

                                     int P { get; } = 7;
                                     event System.Action? E = null;

                                     void M( int p ) { }
                                 }
                                 """;

    /// <summary>
    /// Verifies that an inspection-only expression has the given type, that it gives the value of a constant initializer, including an enumeration
    /// member, and that a non-constant initializer has no constant value but has its source text.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>null</c> and <c>default</c> literals give the default constant of the type of the expression.
    /// </summary>
    [Fact]
    public void CreateInspectionOnly_NullAndDefault_AsTypedConstant()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var nullExpression = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_null" ), GetFieldType( compilation, "_null" ) );

        var defaultExpression = SourceExpressionFactory.CreateInspectionOnly(
            GetInitializer( compilation, "_default" ),
            GetFieldType( compilation, "_default" ) );

        var nullConstant = nullExpression.AsTypedConstant;
        var defaultConstant = defaultExpression.AsTypedConstant;

        Assert.NotNull( nullConstant );
        Assert.Null( nullConstant.Value.Value );
        Assert.Equal( SpecialType.Object, nullConstant.Value.Type.SpecialType );

        Assert.NotNull( defaultConstant );
        Assert.Equal( SpecialType.Int32, defaultConstant.Value.Type.SpecialType );
    }

    /// <summary>
    /// Verifies that the textual representation of the expression, which a debugger or a log message uses, does not report LAMA0297.
    /// </summary>
    [Fact]
    public void CreateInspectionOnly_ToString_DoesNotThrow()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var expression = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_sum" ), GetFieldType( compilation, "_sum" ) );

        Assert.Equal( "1 + 2", expression.ToString() );
    }

    /// <summary>
    /// Verifies that the guard of the syntax tree accepts an expression of a syntax tree that a partial compilation does not include, because the
    /// tree belongs to the Roslyn compilation of the type, which has a semantic model for it.
    /// </summary>
    [Fact]
    public void CreateInspectionOnly_PartialCompilation_AcceptsTreeOfRoslynCompilation()
    {
        using var testContext = this.CreateTestContext();

        var roslynCompilation = testContext.CreateCSharpCompilation(
            new System.Collections.Generic.Dictionary<string, string> { ["A.cs"] = "class A { int _a = 1; }", ["B.cs"] = "class B { int _b = 2; }" } );

        var partialCompilation = PartialCompilation.CreatePartial( roslynCompilation, roslynCompilation.SyntaxTrees.Single( t => t.FilePath == "A.cs" ) );
        var compilation = CompilationModel.CreateInitialInstance( new ProjectModel( roslynCompilation, testContext.ServiceProvider ), partialCompilation );

        var initializerOfB = roslynCompilation.SyntaxTrees.Single( t => t.FilePath == "B.cs" )
            .GetRoot( TestContext.Current.CancellationToken )
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Single()
            .Initializer!.Value;

        var expression = SourceExpressionFactory.CreateInspectionOnly( initializerOfB, compilation.Factory.GetSpecialType( SpecialType.Int32 ) );

        Assert.Equal( 2, expression.AsTypedConstant!.Value.Value );
    }

    /// <summary>
    /// Verifies that the syntax node of an inspection-only expression, given by <c>AsSyntaxNode</c> and by
    /// <see cref="SourceExpressionExtensions.GetSourceSyntax"/>, is the source node.
    /// </summary>
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

    /// <summary>
    /// Verifies that an inspection-only expression is not assignable.
    /// </summary>
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

    /// <summary>
    /// Verifies that an inspection-only expression cannot be made durable, and that the error says so instead of reporting a template error.
    /// </summary>
    [Fact]
    public void CreateInspectionOnly_ToDurable_Throws()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var expression = SourceExpressionFactory.CreateInspectionOnly( GetInitializer( compilation, "_number" ), GetFieldType( compilation, "_number" ) );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var exception = Assert.Throws<InvalidOperationException>( () => expression.ToDurable() );

            Assert.Contains( "inspection", exception.Message, StringComparison.Ordinal );
        }
    }

    /// <summary>
    /// Verifies that an expression whose syntax tree does not belong to the compilation is refused with an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void CreateInspectionOnly_ForeignSyntaxTree_Throws()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        Assert.Throws<ArgumentException>(
            () => SourceExpressionFactory.CreateInspectionOnly( SyntaxFactory.ParseExpression( "42" ), GetFieldType( compilation, "_number" ) ) );
    }

    /// <summary>
    /// Verifies that the source syntax of the initializer of a field is the source node of the initializer.
    /// </summary>
    [Fact]
    public void GetSourceSyntax_FieldInitializer_ReturnsSourceNode()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var field = compilation.Types.OfName( "C" ).Single().Fields.OfName( "_number" ).Single();

        Assert.Same( GetInitializer( compilation, "_number" ), field.InitializerExpression.AssertNotNull().GetSourceSyntax() );
    }

    /// <summary>
    /// Verifies that the source syntax of the initializer of a property and of an event field is the expression of the initializer.
    /// </summary>
    [Fact]
    public void GetSourceSyntax_PropertyAndEventInitializers_ReturnSourceNode()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var type = compilation.Types.OfName( "C" ).Single();

        var propertySyntax = type.Properties.OfName( "P" ).Single().InitializerExpression.AssertNotNull().GetSourceSyntax();
        var eventSyntax = type.Events.OfName( "E" ).Single().InitializerExpression.AssertNotNull().GetSourceSyntax();

        Assert.Equal( "7", propertySyntax?.ToString() );
        Assert.IsType<PropertyDeclarationSyntax>( propertySyntax?.Parent?.Parent );
        Assert.Equal( "null", eventSyntax?.ToString() );
        Assert.IsType<VariableDeclaratorSyntax>( eventSyntax?.Parent?.Parent );
    }

    /// <summary>
    /// Verifies that <see cref="SourceExpressionExtensions.GetSourceSyntax"/> returns <c>null</c> for a parameter.
    /// </summary>
    [Fact]
    public void GetSourceSyntax_Parameter_ReturnsNull()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var parameter = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single().Parameters[0];

        Assert.Null( parameter.GetSourceSyntax() );
    }

    /// <summary>
    /// Verifies that <see cref="SourceExpressionExtensions.GetSourceSyntax"/> returns <c>null</c> for a typed constant.
    /// </summary>
    [Fact]
    public void GetSourceSyntax_TypedConstant_ReturnsNull()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        Assert.Null( TypedConstant.Create( 1, compilation.Factory.GetSpecialType( SpecialType.Int32 ) ).GetSourceSyntax() );
    }

    /// <summary>
    /// Verifies that <see cref="SourceExpressionExtensions.GetSourceSyntax"/> returns <c>null</c> for an expression built by
    /// <see cref="ExpressionFactory"/>.
    /// </summary>
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

    /// <summary>
    /// Returns the initializer expression of the variable declarator of the given name.
    /// </summary>
    private static ExpressionSyntax GetInitializer( CompilationModel compilation, string fieldName )
        => compilation.RoslynCompilation.SyntaxTrees
            .SelectMany( t => t.GetRoot().DescendantNodes() )
            .OfType<VariableDeclaratorSyntax>()
            .Single( v => v.Identifier.Text == fieldName )
            .Initializer!.Value;

    /// <summary>
    /// Returns the type of a field of the type <c>C</c>.
    /// </summary>
    private static IType GetFieldType( CompilationModel compilation, string fieldName )
        => compilation.Types.OfName( "C" ).Single().Fields.OfName( fieldName ).Single().Type;
}
