// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Advising;

/// <summary>
/// A strategy that defines how an introduced constructor parameter should be propagated (or "pulled") to child constructors in derived classes.
/// For standard implementations, see <see cref="PullStrategy"/>.
/// </summary>
/// <remarks>
/// <para>
/// When you introduce a parameter to a base constructor using <see cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, TypedConstant, Metalama.Framework.Advising.IPullStrategy?, System.Collections.Immutable.ImmutableArray{Metalama.Framework.Code.DeclarationBuilders.AttributeConstruction})"/>
/// or the corresponding extension methods in <see cref="AdviserExtensions"/>, this strategy determines how child constructors (in derived classes or in the same class)
/// should obtain the value for this parameter when calling the base constructor.
/// </para>
/// <para>
/// <strong>Cross-Project Operation:</strong> Pull strategies operate across project boundaries. Custom implementations
/// must be serializable (implement <see cref="ICompileTimeSerializable"/>) because the strategy needs to be persisted
/// and applied in projects that reference the project where the parameter was introduced. For example, if you introduce
/// a parameter to a base class in Project A using <see cref="PullStrategy.IntroduceParameterAndPull"/>, and Project B
/// references Project A and has derived classes, those derived classes will automatically have the parameter added to
/// their constructors.
/// </para>
/// </remarks>
/// <seealso cref="PullStrategy"/>
/// <seealso cref="PullAction"/>
/// <seealso cref="AdviserExtensions.IntroduceParameter(IAdviser{IConstructor}, string, IType, TypedConstant, Metalama.Framework.Advising.IPullStrategy?, System.Collections.Immutable.ImmutableArray{Metalama.Framework.Code.DeclarationBuilders.AttributeConstruction})"/>
/// <seealso href="@introducing-constructor-parameters"/>
public interface IPullStrategy : ICompileTimeSerializable
{
    /// <summary>
    /// Gets the <see cref="PullAction"/> that specifies how to obtain the value for an introduced parameter
    /// when it needs to be passed from a child constructor to the constructor where it was introduced.
    /// </summary>
    /// <param name="pulledParameter">The parameter that was introduced in the parent constructor and needs to be passed a value.</param>
    /// <param name="targetMember">The child constructor or method that needs to provide a value for <paramref name="pulledParameter"/>.</param>
    /// <returns>A <see cref="PullAction"/> that specifies how the child constructor should obtain the value for the introduced parameter.</returns>
    PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember );
}