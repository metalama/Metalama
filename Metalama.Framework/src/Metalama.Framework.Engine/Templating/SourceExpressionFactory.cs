// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// Creates expressions of the code model that wrap source syntax.
/// </summary>
[PublicAPI]
public static class SourceExpressionFactory
{
    /// <summary>
    /// Creates an <see cref="ISourceExpression"/> over a source expression, for inspection by compile-time code. The expression cannot be emitted
    /// in generated code: an attempt to do so during a template expansion reports LAMA0297.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The members of <see cref="ISourceExpression"/> behave as for any source expression: <see cref="ISourceExpression.AsSyntaxNode"/> returns
    /// <paramref name="expression"/>, and <see cref="ISourceExpression.AsTypedConstant"/> returns the value of a literal or of an enumeration member.
    /// The expression is not assignable.
    /// </para>
    /// <para>
    /// Emitting the expression is refused because it is already evaluated at its original location: a second evaluation can have side effects,
    /// and the expression can reference local variables and parameters that do not exist where it would be emitted.
    /// </para>
    /// <para>
    /// For the same reason, the expression cannot be made durable: <see cref="Metalama.Framework.Code.ExpressionExtensions.ToDurable(IExpression)"/> throws an
    /// <see cref="InvalidOperationException"/>. The expression references the syntax tree and the compilation, so it must not be stored in an
    /// object that outlives the request in which it was created.
    /// </para>
    /// </remarks>
    /// <param name="expression">An expression of a syntax tree of the compilation of <paramref name="type"/>.</param>
    /// <param name="type">The type of the expression, in the compilation that contains the syntax tree.</param>
    /// <exception cref="ArgumentException">The syntax tree of <paramref name="expression"/> is not a syntax tree of the compilation of
    /// <paramref name="type"/>. After a low-level weaver, the compilations of the later stages have new instances of the syntax trees, so the
    /// type must come from the compilation that contains <paramref name="expression"/>, for instance the source compilation.</exception>
    public static ISourceExpression CreateInspectionOnly( ExpressionSyntax expression, IType type )
    {
        if ( expression == null )
        {
            throw new ArgumentNullException( nameof(expression) );
        }

        if ( type == null )
        {
            throw new ArgumentNullException( nameof(type) );
        }

        if ( !type.GetCompilationModel().RoslynCompilation.ContainsSyntaxTree( expression.SyntaxTree ) )
        {
            throw new ArgumentException( $"The expression '{expression}' does not belong to a syntax tree of the compilation of the type '{type}'.", nameof(expression) );
        }

        return new SourceUserExpression( expression, type, isInspectionOnly: true );
    }
}
