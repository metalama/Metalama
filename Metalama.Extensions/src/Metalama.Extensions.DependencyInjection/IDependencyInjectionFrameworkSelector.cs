// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;
using System.Collections.Immutable;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Exposes a method <see cref="SelectFramework"/> that gets called when there are many candidate frameworks for a dependency.
/// </summary>
[CompileTime]
public interface IDependencyInjectionFrameworkSelector : ICompileTimeSerializable
{
    /// <summary>
    /// Selects the <see cref="IDependencyInjectionFramework"/> that should handle a given dependency.
    /// </summary>
    /// <param name="properties">The properties of this dependencies.</param>
    /// <param name="eligibleFrameworks">The list of frameworks that are eligible for this dependency.</param>
    /// <returns>The selected dependency.</returns>
    IDependencyInjectionFramework SelectFramework( DependencyProperties properties, ImmutableArray<IDependencyInjectionFramework> eligibleFrameworks );
}