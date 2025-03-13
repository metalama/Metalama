// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability;

/// <summary>
/// Specifies that the <see cref="ObservableAttribute"/> aspect must not report warnings when it meets an unsupported exception in the target
/// type or member. These warnings can otherwise be suppressed using the <c>#pragma warning disable</c> directive.
/// </summary>
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class )]
public sealed class SuppressObservabilityWarningsAttribute : Attribute, IHierarchicalOptionsProvider
{
    private readonly bool _suppressWarnings;

    public SuppressObservabilityWarningsAttribute( bool suppressWarnings = true )
    {
        this._suppressWarnings = suppressWarnings;
    }

    IEnumerable<IHierarchicalOptions> IHierarchicalOptionsProvider.GetOptions( in OptionsProviderContext context )
        => new[] { new DependencyAnalysisOptions() { SuppressWarnings = this._suppressWarnings } };
}