// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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