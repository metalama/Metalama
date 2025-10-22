// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// Interface used by <see cref="DefaultDependencyInjectionStrategy"/> to introduce and pull a constructor parameter.
/// Implementations must be serializable, as they implement <see cref="ICompileTimeSerializable"/>.
/// </summary>
public interface IParameterPullStrategy : IPullStrategy
{
    /// <summary>
    /// Gets an existing parameter that satisfies the dependency, if any.
    /// </summary>
    /// <param name="constructor">The constructor in which the dependency is required.</param>
    /// <returns>A parameter satisfying the dependency, or <c>null</c>.</returns>
    IParameter? GetExistingParameter( IConstructor constructor );

    /// <summary>
    /// Gets a <see cref="ParameterSpecification"/> object specifying how to create a parameter.
    /// This method is called if <see cref="GetExistingParameter"/> returns <c>null</c>.
    /// </summary>
    /// <param name="constructor">The constructor in which the parameter must be added.</param>
    /// <returns>A <see cref="ParameterSpecification"/>.</returns>
    ParameterSpecification GetNewParameter( IConstructor constructor );
}