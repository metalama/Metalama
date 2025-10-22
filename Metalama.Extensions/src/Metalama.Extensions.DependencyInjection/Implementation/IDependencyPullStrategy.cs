// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// Interface used by <see cref="DefaultDependencyInjectionStrategy"/> to pull a field or property from the constructor.
/// This interface is instantiated from <see cref="DefaultDependencyInjectionStrategy.GetDependencyPullStrategy"/>. You must override this method
/// if you want to provide a custom implementation of <see cref="IDependencyPullStrategy"/>. The default implementation is <see cref="DefaultDependencyPullStrategy"/>.
/// </summary>
[CompileTime]
public interface IDependencyPullStrategy
{
    /// <summary>
    /// Gets a statement that assigns the dependency field or property from a parameter or another expression.
    /// </summary>
    /// <param name="existingParameter">The value returned by <see cref="IParameterPullStrategy.GetExistingParameter"/> or <see cref="IParameterPullStrategy.GetNewParameter"/>.</param>
    IStatement GetAssignmentStatement( IParameter existingParameter );

    /// <summary>
    /// Creates an <see cref="IParameterPullStrategy"/>, used to introduce and pull the parameter.
    /// </summary>
    IParameterPullStrategy CreateParameterPullStrategy();
}