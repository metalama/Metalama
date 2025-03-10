// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents an aspect class (i.e. a type of aspect) and exposes all its instances in the current scope.
/// </summary>
public interface IIntrospectionAspectClass : IAspectClass
{
    /// <summary>
    /// Gets the instances of the aspect class in the current scope.
    /// </summary>
    ImmutableArray<IIntrospectionAspectInstance> Instances { get; }
}