// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.CompileTimeContracts;

[PublicAPI]
public readonly struct TypedExpressionSyntax
{
    internal ITypedExpressionSyntaxImpl Implementation { get; }

    internal TypedExpressionSyntax( ITypedExpressionSyntaxImpl impl )
    {
        this.Implementation = impl;
    }

    internal IType? ExpressionType => this.Implementation.ExpressionType;

    public ExpressionSyntax Syntax => this.Implementation.Syntax;

    public bool? IsReferenceable => this.Implementation.IsReferenceable;

    public static implicit operator ExpressionSyntax( TypedExpressionSyntax runtimeExpression ) => runtimeExpression.Syntax;

    public static implicit operator ExpressionStatementSyntax?( TypedExpressionSyntax runtimeExpression ) => runtimeExpression.Implementation.ToStatement();

    public IUserExpression ToUserExpression( ICompilation compilation ) => this.Implementation.ToUserExpression( compilation );

    public override string ToString() => this.Implementation.ToString();
}