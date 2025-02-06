// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Immutability.Configuration;

internal sealed class ImmutabilityOptions : IHierarchicalOptions<INamespace>, IHierarchicalOptions<INamedType>
{
    public ImmutabilityKind? Kind { get; init; }

    public IImmutabilityClassifier? Classifier { get; init; }

    public object ApplyChanges( object changes, in ApplyChangesContext context )
    {
        var typedChanges = (ImmutabilityOptions) changes;

        // A property (Kind or Classifier), when defined, nullifies the other property.

        return new ImmutabilityOptions()
        {
            Kind = typedChanges.Classifier != null ? null : typedChanges.Kind ?? this.Kind,
            Classifier = typedChanges.Kind != null ? null : typedChanges.Classifier ?? this.Classifier
        };
    }

    public IHierarchicalOptions? GetDefaultOptions( OptionsInitializationContext context ) => null;
}