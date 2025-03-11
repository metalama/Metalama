// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class SyntaxVariableExpression( ExpressionSyntax expression, IType type, RefKind refKind )
    : SyntaxUserExpression( expression, type, isReferenceable: true, isAssignable: refKind is not (RefKind.In or RefKind.RefReadOnly) )
{
    public override RefKind RefKind { get; } = refKind;
}