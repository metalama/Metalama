// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.CompileTimeContracts;

internal interface ITypedExpressionSyntaxImpl
{
    IType? ExpressionType { get; }

    ExpressionSyntax? Syntax { get; }

    ExpressionStatementSyntax? ToStatement();

    IUserExpression ToUserExpression( ICompilation compilation );

    bool? IsReferenceable { get; }
}