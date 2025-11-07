// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents a tuple type.
/// </summary>
public interface ITupleType : INamedType
{
    /// <summary>
    /// Gets the elements of the tuple.
    /// </summary>
    IReadOnlyList<ITupleElement> TupleElements { get; }

    /// <summary>
    /// Gets the number of elements in the tuple.
    /// </summary>
    int TupleLength { get; }

    /// <summary>
    /// Creates an <see cref="IExpression"/> that creates an instance of the tuple with the specified values, given as <see cref="IExpression"/>.
    /// </summary>
    IExpression CreateCreateInstanceExpression( params IEnumerable<IExpression> values );

    /// <summary>
    /// Creates an <see cref="IExpression"/> that creates an instance of the tuple with the specified values.
    /// </summary>
    IExpression CreateCreateInstanceExpression( params dynamic?[] values );
}