// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class OverrideTypeUserExpression : UserExpression
{
    private readonly IExpression _expression;

    public OverrideTypeUserExpression( IExpression expression, IType type )
    {
        this._expression = expression;
        this.Type = type;
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        var expression = this._expression.ToExpressionSyntax( syntaxSerializationContext, targetType );

        var expressionWithNewTypeAnnotation = TypeAnnotationMapper.AddExpressionTypeAnnotation( expression, this.Type );

        return expressionWithNewTypeAnnotation;
    }

    public override IType Type { get; }
}