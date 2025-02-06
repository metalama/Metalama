// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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