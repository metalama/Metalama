// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Observability.Configuration;

internal sealed record DependencyAnalysisOptions :
    IHierarchicalOptions<ICompilation>,
    IHierarchicalOptions<INamespace>,
    IHierarchicalOptions<INamedType>,
    IHierarchicalOptions<IMember>
{
    /// <summary>
    /// Gets a value whether observability warnings in the target members must be suppressed.
    /// </summary>
    public bool? SuppressWarnings { get; init; }

    /// <summary>
    /// Gets an <see cref="ObservabilityContract"/> for the target member, guaranteeing its behavior
    /// with respect to the <see cref="ObservableAttribute"/> aspect.
    /// </summary>
    public ObservabilityContract? ObservabilityContract { get; init; }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (DependencyAnalysisOptions) changes;

        return new DependencyAnalysisOptions
        {
            SuppressWarnings = other.SuppressWarnings ?? this.SuppressWarnings,
            ObservabilityContract = other.ObservabilityContract ?? this.ObservabilityContract
        };
    }

    internal static DependencyAnalysisOptions Default { get; } = new()
    {
        SuppressWarnings = false

        // Other members intentionally null by default because we could have some rules given the default.
    };

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context ) => Default;
}