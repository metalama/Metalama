// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class ExpressionFactoryTests : UnitTestClass
{
    private static readonly SyntaxGenerationOptions _syntaxGenerationOptions = new( CodeFormattingOptions.Default );

    private sealed record ExpressionInfo( string Syntax, IType? Type );

    protected override void AddSyntaxGenerationOptions( IAdditionalServiceCollection services )
    {
        services.AddProjectService( SyntaxGenerationOptions.Unformatted );
    }

    private ExpressionInfo GetExpression( Func<IExpression> f, string code = "" )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( code );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var syntaxGenerationContext =
                new SyntaxSerializationContext( compilation, _syntaxGenerationOptions );

            var expression = (IUserExpression) f();

            return new ExpressionInfo( expression.ToTypedExpressionSyntax( syntaxGenerationContext ).Syntax.ToString(), expression.Type );
        }
    }

    /// <summary>
    /// Verifies that <see cref="ExpressionFactory.This(INamedType)"/> is written as <c>this</c> outside a template, for instance in the provider of
    /// an extension, where there is no aspect layer to reference.
    /// </summary>
    [Fact]
    public void ThisOutsideTemplate()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        using ( testContext.WithExecutionContext( compilation ) )
        {
            var type = compilation.Types.OfName( "C" ).Single();
            var expression = (IUserExpression) ExpressionFactory.This( type );
            var syntax = expression.ToTypedExpressionSyntax( new SyntaxSerializationContext( compilation, _syntaxGenerationOptions ) ).Syntax;

            Assert.Equal( "this", syntax.ToString() );
            Assert.Same( type, expression.Type );
        }
    }

    [Fact]
    public void UntypedNull()
    {
        var expression = this.GetExpression( ExpressionFactory.Null );

        Assert.Equal( "null", expression.Syntax );

        // The expression should be untyped (target typed) but the Metalama model does not allow for it.
        Assert.Equal( "object?", expression.Type.AssertNotNull().ToString() );
    }

    [Fact]
    public void TypedNull()
    {
        var expression = this.GetExpression( ExpressionFactory.Null<string> );

        Assert.Equal( "null", expression.Syntax );
        Assert.Equal( "string?", expression.Type.AssertNotNull().ToString() );
    }

    [Fact]
    public void UntypedDefault()
    {
        var expression = this.GetExpression( ExpressionFactory.Default );

        Assert.Equal( "default", expression.Syntax );

        // The expression should be untyped (target typed) but the Metalama model does not allow for it.
        Assert.Equal( "object?", expression.Type.AssertNotNull().ToString() );
    }

    [Fact]
    public void ReferenceTypedDefault()
    {
        var expression = this.GetExpression( ExpressionFactory.Default<string> );

        Assert.Equal( "default(global::System.String)", expression.Syntax );
        Assert.Equal( "string?", expression.Type.AssertNotNull().ToString() );
    }

    [Fact]
    public void ValueTypedDefault()
    {
        var expression = this.GetExpression( ExpressionFactory.Default<int> );

        Assert.Equal( "default(global::System.Int32)", expression.Syntax );
        Assert.Equal( "int", expression.Type.AssertNotNull().ToString() );
    }

    [Fact]
    public void NullObjectLiteral()
    {
        var expression = this.GetExpression( () => ExpressionFactory.Literal( (object?) null ) );

        Assert.Equal( "null", expression.Syntax );
        Assert.Equal( "string?", expression.Type.AssertNotNull().ToString() );
    }

    [Fact]
    public void IntObjectLiteral()
    {
        var expression = this.GetExpression( () => ExpressionFactory.Literal( (object?) 5 ) );

        Assert.Equal( "5", expression.Syntax );
        Assert.Equal( "int", expression.Type.AssertNotNull().ToString() );
    }
}