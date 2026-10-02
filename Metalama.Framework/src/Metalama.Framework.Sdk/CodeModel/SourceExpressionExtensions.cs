// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.CodeModel;

/// <summary>
/// Extension methods that give access to the Roslyn syntax of expressions of the code model.
/// </summary>
[PublicAPI]
public static class SourceExpressionExtensions
{
    /// <summary>
    /// Gets the source <see cref="ExpressionSyntax"/> of an expression that wraps source syntax, or <c>null</c> when the expression does not wrap
    /// source syntax, for example a generated expression or a parameter.
    /// </summary>
    /// <remarks>
    /// The method is a typed view of <see cref="ISourceExpression.AsSyntaxNode"/>. It returns the node of the source syntax tree, without the cast
    /// that the code generation can add.
    /// </remarks>
    /// <param name="expression">The expression.</param>
    public static ExpressionSyntax? GetSourceSyntax( this IExpression expression )
        => expression is ISourceExpression { AsSyntaxNode: ExpressionSyntax syntax } ? syntax : null;
}
