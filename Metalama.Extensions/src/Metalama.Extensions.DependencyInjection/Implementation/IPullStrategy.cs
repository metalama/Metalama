// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// Interface used by <see cref="DefaultDependencyInjectionStrategy"/> to pull a field or property from the constructor.
/// This interface is instantiated from <see cref="DefaultDependencyInjectionStrategy.GetPullStrategy"/>. You must override this method
/// if you want to provide a custom implementation of <see cref="IPullStrategy"/>. The default implementation is <see cref="DefaultPullStrategy"/>.
/// </summary>
[CompileTime]
public interface IPullStrategy
{
    /// <summary>
    /// Gets a parameter from which the dependency can be initialized, or <c>null</c> if a new parameter
    /// must be created.
    /// </summary>
    IParameter? GetExistingParameter( IConstructor constructor );

    /// <summary>
    /// Gets the specifications from which a constructor parameter can be constructed. This method is called when <see cref="GetExistingParameter"/>
    /// returns <c>null</c>.
    /// </summary>
    /// <param name="constructor">The constructor into which the parameter will be added.</param>
    /// <returns></returns>
    ParameterSpecification GetNewParameter( IConstructor constructor );

    /// <summary>
    /// Returns a <see cref="PullAction"/> that instructs how a given constructor parameter should be pulled from another constructor.
    /// </summary>
    PullAction PullParameter( IParameter calledConstructorParameter, IConstructor callingConstructor );

    /// <summary>
    /// Gets a statement that assigns the dependency field or property from a parameter or another expression.
    /// </summary>
    /// <param name="existingParameter">The value returned by <see cref="GetExistingParameter"/> or <see cref="GetNewParameter"/>.</param>
    IStatement GetAssignmentStatement( IParameter existingParameter );
}