// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class SyntaxVariableExpression( ExpressionSyntax expression, IType type, RefKind refKind )
    : SyntaxUserExpression( expression, type, isReferenceable: true, isAssignable: refKind is not (RefKind.In or RefKind.RefReadOnly) )
{
    public override RefKind RefKind { get; } = refKind;
}