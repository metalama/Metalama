// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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