// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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