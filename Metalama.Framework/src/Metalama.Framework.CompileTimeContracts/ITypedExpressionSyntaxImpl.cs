// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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