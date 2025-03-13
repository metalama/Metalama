// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability;

/// <summary>
/// Adds an observability contract that guarantees that the outputs of the method depend neither on any non-constant
/// property of input arguments nor on any other non-input factor.
/// When applied to a type, the guarantee must hold for all methods.
/// </summary>
[AttributeUsage( AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class )]
public sealed class ConstantAttribute : Attribute, IHierarchicalOptionsProvider
{
    IEnumerable<IHierarchicalOptions> IHierarchicalOptionsProvider.GetOptions( in OptionsProviderContext context )
        => new[] { new DependencyAnalysisOptions() { ObservabilityContract = ObservabilityContract.Constant } };
}