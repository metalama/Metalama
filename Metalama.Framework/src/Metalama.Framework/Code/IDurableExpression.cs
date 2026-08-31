// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code;

/// <summary>
/// A representation of an <see cref="IExpression"/> that is safe to be held across compilations. Obtain one by
/// calling <see cref="ExpressionExtensions.ToDurable"/>, and turn it back into an <see cref="IExpression"/> by
/// calling <see cref="ToExpression"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is to <see cref="IExpression"/> what <see cref="IDurableRef{T}"/> is to <see cref="IRef{T}"/>. An
/// <see cref="IExpression"/> is not durable: one captured in a template holds its compilation, and one read from the
/// code model holds the syntax tree of its source file. Aspect state that must survive an edit cannot hold either.
/// </para>
/// <para>
/// The conversion renders the expression immediately and keeps the result, so it works for every kind of expression.
/// </para>
/// </remarks>
[CompileTime]
[InternalImplement]
[Durable]
[ImmutableType]
public interface IDurableExpression
{
    /// <summary>
    /// Gets a durable reference to the type of the expression, which is the counterpart of
    /// <see cref="IHasType.Type"/>.
    /// </summary>
    IDurableRef<IType> Type { get; }

    /// <summary>
    /// Gets a value indicating whether the value of the expression can be set, which is the counterpart of
    /// <see cref="IExpression.IsAssignable"/>.
    /// </summary>
    bool IsAssignable { get; }

    /// <summary>
    /// Gets the C# code of the expression, which is what a serializer of aspect state persists.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Returns an <see cref="IExpression"/> for a given compilation.
    /// </summary>
    /// <param name="compilation">The compilation in which the expression is to be used, and against which
    /// <see cref="Type"/> is resolved.</param>
    IExpression ToExpression( ICompilation compilation );
}