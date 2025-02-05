// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents a fabric.
/// </summary>
public interface IIntrospectionFabric : IIntrospectionAspectPredecessor
{
    /// <summary>
    /// Gets the full name of the fabric type.
    /// </summary>
    string FullName { get; }
}