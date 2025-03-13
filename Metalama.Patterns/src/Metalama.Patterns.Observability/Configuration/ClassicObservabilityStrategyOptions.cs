// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Options specific to the "natural" implementation.
/// </summary>
[PublicAPI]
[CompileTime]
internal sealed record ClassicObservabilityStrategyOptions : IHierarchicalOptions<ICompilation>, IHierarchicalOptions<INamespace>,
                                                             IHierarchicalOptions<INamedType>
{
    public bool? EnableOnObservablePropertyChangedMethod { get; init; }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var other = (ClassicObservabilityStrategyOptions) changes;

        return new ClassicObservabilityStrategyOptions
        {
            EnableOnObservablePropertyChangedMethod = other.EnableOnObservablePropertyChangedMethod
                                                      ?? this.EnableOnObservablePropertyChangedMethod
        };
    }

    IHierarchicalOptions IHierarchicalOptions.GetDefaultOptions( OptionsInitializationContext context )
        => new ClassicObservabilityStrategyOptions() { EnableOnObservablePropertyChangedMethod = true };
}