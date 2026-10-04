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
    /// <para>
    /// The method is a typed view of <see cref="ISourceExpression.AsSyntaxNode"/>. It returns the node of the source syntax tree, without the cast
    /// that the code generation can add.
    /// </para>
    /// <para>
    /// The public API of <c>Metalama.Framework</c> does not reference Roslyn, so <see cref="ISourceExpression.AsSyntaxNode"/> is typed
    /// <see cref="object"/>, and a <see cref="ISourceExpression"/> is not always a source expression whose node is an
    /// <see cref="ExpressionSyntax"/>. This method gives code that references the SDK, for instance an extension or the provider of an
    /// interceptor, the syntax of a source expression that the code model exposes: the initializer of a field, a property or an event, or an
    /// inspection-only expression created by <c>SourceExpressionFactory.CreateInspectionOnly</c>, such as an argument of an intercepted call.
    /// The caller can then inspect the syntax, for instance to recognize a lambda, an interpolated string or a <c>nameof</c> expression, which
    /// <see cref="IExpression"/> does not describe.
    /// </para>
    /// </remarks>
    /// <param name="expression">The expression.</param>
    public static ExpressionSyntax? GetSourceSyntax( this IExpression expression )
        => expression is ISourceExpression { AsSyntaxNode: ExpressionSyntax syntax } ? syntax : null;
}
