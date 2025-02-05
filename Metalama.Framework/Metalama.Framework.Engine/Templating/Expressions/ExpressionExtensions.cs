// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal static class ExpressionExtensions
{
    public static ExpressionSyntax ToExpressionSyntax( this IUserExpression userExpression, SyntaxSerializationContext context, IType? targetType = null )
        => userExpression.ToTypedExpressionSyntax( context, targetType ).Syntax;

    public static IUserExpression ToUserExpression( this IExpression expression )
        => expression switch
        {
            null => null!,
            IUserExpression userExpression => userExpression,
            TypedConstant typedConstant => (IUserExpression) SyntaxBuilder.CurrentImplementation.TypedConstant( typedConstant ),
            _ => throw new ArgumentException( $"Expression of type '{expression.GetType()}' could not be converted to IUserExpression." )
        };

    private static IUserExpression ToUserExpression( this IExpression expression, ISyntaxGenerationContext syntaxGenerationContext )
        => expression switch
        {
            null => null!,
            IUserExpression userExpression => userExpression,
            TypedConstant typedConstant => new SyntaxUserExpression(
                ((SyntaxSerializationContext) syntaxGenerationContext).SyntaxGenerator.TypedConstant( typedConstant ),
                typedConstant.Type ),
            _ => throw new ArgumentException( $"Expression of type '{expression.GetType()}' could not be converted to IUserExpression." )
        };

    public static TypedExpressionSyntax ToTypedExpressionSyntax(
        this IExpression expression,
        ISyntaxGenerationContext syntaxGenerationContext,
        IType? targetType = null )
        => expression.ToUserExpression( syntaxGenerationContext ).ToTypedExpressionSyntax( syntaxGenerationContext, targetType );

    public static ExpressionSyntax ToExpressionSyntax( this IExpression expression, SyntaxSerializationContext context, IType? targetType = null )
        => expression.ToTypedExpressionSyntax( context, targetType ).Syntax;
}