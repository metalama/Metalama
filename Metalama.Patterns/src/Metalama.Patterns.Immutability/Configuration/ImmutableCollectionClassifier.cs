// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;

namespace Metalama.Patterns.Immutability.Configuration;

internal sealed class ImmutableCollectionClassifier : IImmutabilityClassifier
{
    public ImmutabilityKind GetImmutabilityKind( INamedType type )
    {
        foreach ( var typeArgument in type.TypeArguments )
        {
            if ( typeArgument.GetImmutabilityKind() != ImmutabilityKind.Deep )
            {
                return ImmutabilityKind.Shallow;
            }
        }

        return ImmutabilityKind.Deep;
    }
}