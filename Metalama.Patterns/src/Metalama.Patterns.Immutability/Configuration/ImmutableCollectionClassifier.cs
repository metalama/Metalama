// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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