// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Utilities.ObjectGraph;

/// <summary>
/// The budgets that bound an <see cref="ObjectGraphWalker"/>.
/// </summary>
/// <remarks>
/// A walk over an arbitrary object graph has no natural bound, therefore every walk is limited both in the number of
/// objects it visits and in its duration. A walk that reaches either limit reports
/// <see cref="ObjectGraphWalkResult.IsExhausted"/>, which makes a negative result inconclusive rather than
/// authoritative.
/// </remarks>
internal sealed record ObjectGraphWalkerOptions
{
    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static ObjectGraphWalkerOptions Default { get; } = new();

    /// <summary>
    /// Gets the maximum number of distinct objects visited before the walk gives up. The default is 400,000.
    /// </summary>
    public int MaxObjects { get; init; } = 400_000;

    /// <summary>
    /// Gets the maximum number of elements inspected in a single array. The default is 100,000.
    /// </summary>
    public int MaxArrayElements { get; init; } = 100_000;

    /// <summary>
    /// Gets the maximum duration of the walk. The default is one minute.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes( 1 );
}
