// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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