// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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