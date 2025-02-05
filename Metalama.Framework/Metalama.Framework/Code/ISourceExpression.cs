// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code;

/// <summary>
/// Represents an <see cref="IExpression"/> defined in source code, as opposed to a generated expression.
/// </summary>
public interface ISourceExpression : IExpression
{
    /// <summary>
    /// Gets the source Roslyn object representing the current expression.
    /// </summary>
    object AsSyntaxNode { get; }

    /// <summary>
    /// Gets the string representing the current expression, with normalize whitespaces.
    /// </summary>
    string AsString { get; }

    /// <summary>
    /// Gets the string representing the current expression, including original whitespaces.
    /// </summary>
    string AsFullString { get; }

    /// <summary>
    /// Gets the <see cref="TypedConstant"/> corresponding to the current expression, or <c>null</c>
    /// if the current expression cannot be represented as a <see cref="TypedConstant"/>.
    /// </summary>
    TypedConstant? AsTypedConstant { get; }
}