// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class ParameterExpression : UserExpression
{
    private readonly IParameter _parameter;

    public ParameterExpression( IParameter parameter )
    {
        this._parameter = parameter;
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        => SyntaxFactory.IdentifierName( this._parameter.Name );

    public override IType Type => this._parameter.Type;
}