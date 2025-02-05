// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class DelegateUserExpression : UserExpression
{
    private readonly Func<SyntaxSerializationContext, ExpressionSyntax> _createExpression;

    public DelegateUserExpression(
        Func<SyntaxSerializationContext, ExpressionSyntax> createExpression,
        IType type,
        bool isReferenceable = false,
        bool isAssignable = false )
    {
        this._createExpression = createExpression;
        this.Type = type;
        this.IsAssignable = isAssignable;
        this.IsReferenceable = isReferenceable;
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        => this._createExpression( syntaxSerializationContext );

    public override IType Type { get; }

    protected override bool? IsAssignable { get; }

    private protected override bool? IsReferenceable { get; }

    protected override string ToStringCore() => "<late bound>";
}