// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability;

/// <summary>
/// Marks a method or type as constant, indicating that its output depends solely on its input parameters and contains
/// no side effects that affect observable properties.
/// </summary>
/// <remarks>
/// <para>
/// When the <see cref="ObservableAttribute"/> aspect encounters a call to a method from a different type, it reports
/// a warning unless the method is known to be constant. A constant method guarantees that subsequent calls with identical
/// arguments will always return the same value and will not affect any observable state.
/// </para>
/// <para>
/// Methods are automatically considered constant when all their input parameters (including <c>this</c> for instance methods)
/// are of immutable types, or when the method is a <c>void</c> method without <c>out</c> arguments.
/// </para>
/// <para>
/// When applied to a type, all methods within that type are considered constant.
/// </para>
/// <para>
/// As an alternative to this attribute, use
/// <see cref="Configuration.ObservabilityExtensions.ConfigureObservability(Metalama.Framework.Fabrics.IQuery{Metalama.Framework.Code.ICompilation},System.Action{Configuration.ObservabilityTypeOptionsBuilder})"/>
/// to mark multiple methods as constant using a fabric.
/// </para>
/// </remarks>
/// <seealso cref="ObservableAttribute"/>
/// <seealso cref="Configuration.ObservabilityContract"/>
/// <seealso href="@observability"/>
[AttributeUsage( AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class )]
public sealed class ConstantAttribute : Attribute, IHierarchicalOptionsProvider
{
    IEnumerable<IHierarchicalOptions> IHierarchicalOptionsProvider.GetOptions( in OptionsProviderContext context )
        => new[] { new DependencyAnalysisOptions() { ObservabilityContract = ObservabilityContract.Constant } };
}