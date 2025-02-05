// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Testing.UnitTesting;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class ExpressionFactoryTests : UnitTestClass
{
    private static readonly SyntaxGenerationOptions _syntaxGenerationOptions = new( new CodeFormattingOptions() );

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

    [Fact]
    public void UntypedNull()
    {
        var expression = this.GetExpression( ExpressionFactory.Null );

        Assert.Equal( "null", expression.Syntax );

        // The expression should be untyped (target typed) but the Metalama model does not allow for it.
        Assert.Equal( "object", expression.Type.ToString() );
    }

    [Fact]
    public void TypedNull()
    {
        var expression = this.GetExpression( ExpressionFactory.Null<string> );

        Assert.Equal( "null", expression.Syntax );
        Assert.Equal( "string?", expression.Type.ToString() );
    }

    [Fact]
    public void UntypedDefault()
    {
        var expression = this.GetExpression( ExpressionFactory.Default );

        Assert.Equal( "default", expression.Syntax );

        // The expression should be untyped (target typed) but the Metalama model does not allow for it.
        Assert.Equal( "object", expression.Type.ToString() );
    }

    [Fact]
    public void ReferenceTypedDefault()
    {
        var expression = this.GetExpression( ExpressionFactory.Default<string> );

        Assert.Equal( "default(global::System.String)", expression.Syntax );
        Assert.Equal( "string?", expression.Type.ToString() );
    }

    [Fact]
    public void ValueTypedDefault()
    {
        var expression = this.GetExpression( ExpressionFactory.Default<int> );

        Assert.Equal( "default(global::System.Int32)", expression.Syntax );
        Assert.Equal( "int", expression.Type.ToString() );
    }

    [Fact]
    public void NullObjectLiteral()
    {
        var expression = this.GetExpression( () => ExpressionFactory.Literal( (object?) null ) );

        Assert.Equal( "null", expression.Syntax );
        Assert.Equal( "string?", expression.Type.ToString() );
    }

    [Fact]
    public void IntObjectLiteral()
    {
        var expression = this.GetExpression( () => ExpressionFactory.Literal( (object?) 5 ) );

        Assert.Equal( "5", expression.Syntax );
        Assert.Equal( "int", expression.Type.ToString() );
    }
}