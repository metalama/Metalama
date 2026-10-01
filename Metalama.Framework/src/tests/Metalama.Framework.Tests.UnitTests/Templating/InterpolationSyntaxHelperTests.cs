// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating;

/// <summary>
/// Tests <see cref="InterpolationSyntaxHelper.Fix"/>.
/// </summary>
public sealed class InterpolationSyntaxHelperTests
{
    /// <summary>
    /// The stack size of the threads that the tests create, like in hosts such as Visual Studio.
    /// </summary>
    private const int _threadStackSize = 1024 * 1024;

    /// <summary>
    /// Verifies that <see cref="InterpolationSyntaxHelper.Fix"/> replaces a line break in an interpolation by a space.
    /// </summary>
    [Fact]
    public void LineBreakIsRemoved()
    {
        var interpolation = CreateInterpolation( "a\r\n+ b" );

        var fixedInterpolation = InterpolationSyntaxHelper.Fix( interpolation );

        var text = fixedInterpolation.ToFullString();
        Assert.DoesNotContain( "\n", text, StringComparison.Ordinal );
        Assert.DoesNotContain( "\r", text, StringComparison.Ordinal );
        Assert.Contains( "a", text, StringComparison.Ordinal );
        Assert.Contains( "+ b", text, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that <see cref="InterpolationSyntaxHelper.Fix"/> can process an interpolation that contains a deep expression
    /// and a line break on a thread with a stack of 1 MB (issue #2083).
    /// </summary>
    [Fact]
    public void DeepExpressionWithLineBreak()
    {
        const int callCount = 1400;

        var code = new StringBuilder();
        code.Append( "this" );

        for ( var i = 0; i < callCount; i++ )
        {
            code.Append( i == callCount / 2 ? "\r\n.M()" : ".M()" );
        }

        var interpolation = CreateInterpolation( code.ToString() );

        var text = RunOnThread( () => InterpolationSyntaxHelper.Fix( interpolation ).ToFullString() );

        Assert.DoesNotContain( "\n", text, StringComparison.Ordinal );
        Assert.DoesNotContain( "\r", text, StringComparison.Ordinal );
        Assert.StartsWith( "{this.M().M()", text, StringComparison.Ordinal );
    }

    /// <summary>
    /// Verifies that <see cref="InterpolationSyntaxHelper.Fix"/> adds the spaces that are missing between the tokens of a
    /// generated expression.
    /// </summary>
    [Fact]
    public void GeneratedExpression()
    {
        var interpolation = SyntaxFactory.Interpolation( CreateIsIntExpression( SyntaxFactory.IdentifierName( "x" ) ) );

        var text = InterpolationSyntaxHelper.Fix( interpolation ).ToFullString();

        Assert.Equal( "{x is int}", text );
    }

    /// <summary>
    /// Verifies that <see cref="InterpolationSyntaxHelper.Fix"/> adds the spaces that are missing between the tokens of a
    /// generated expression that contains a deep expression, on a thread with a stack of 1 MB (issue #2083).
    /// </summary>
    [Fact]
    public void DeepGeneratedExpression()
    {
        const int callCount = 1400;

        var code = new StringBuilder();
        code.Append( "this" );

        for ( var i = 0; i < callCount; i++ )
        {
            code.Append( ".M()" );
        }

        var expression = SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalAndExpression,
            SyntaxFactory.ParseExpression( code.ToString() ),
            CreateIsIntExpression( SyntaxFactory.IdentifierName( "x" ) ) );

        var interpolation = SyntaxFactory.Interpolation( expression );

        var text = RunOnThread( () => InterpolationSyntaxHelper.Fix( interpolation ).ToFullString() );

        Assert.StartsWith( "{this.M().M()", text, StringComparison.Ordinal );
        Assert.EndsWith( ".M()&&x is int}", text, StringComparison.Ordinal );
    }

    /// <summary>
    /// Creates the expression <c>operand is int</c> with <see cref="SyntaxFactory"/>, without trivia between the tokens.
    /// </summary>
    private static BinaryExpressionSyntax CreateIsIntExpression( ExpressionSyntax operand )
        => SyntaxFactory.BinaryExpression(
            SyntaxKind.IsExpression,
            operand,
            SyntaxFactory.PredefinedType( SyntaxFactory.Token( SyntaxKind.IntKeyword ) ) );

    /// <summary>
    /// Parses the given expression and returns an interpolation that contains it.
    /// </summary>
    private static InterpolationSyntax CreateInterpolation( string expression )
        => SyntaxFactory.Interpolation( SyntaxFactory.ParseExpression( expression ) );

    /// <summary>
    /// Runs the given function on a new thread with a stack of <see cref="_threadStackSize"/> bytes, and returns its result
    /// or rethrows its exception.
    /// </summary>
    private static T RunOnThread<T>( Func<T> func )
    {
        var result = default(T);
        ExceptionDispatchInfo? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    result = func();
                }
                catch ( Exception e )
                {
                    exception = ExceptionDispatchInfo.Capture( e );
                }
            },
            _threadStackSize );

        thread.Start();
        thread.Join();

        exception?.Throw();

        return result!;
    }
}
