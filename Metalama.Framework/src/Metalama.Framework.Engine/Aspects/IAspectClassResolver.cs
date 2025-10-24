// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

public interface IAspectClassResolver
{
    bool TryGetAspectClass( Type aspectType, [NotNullWhen( true )] out IAspectClass? aspectClass );

    bool TryGetAspectClass( string fullName, [NotNullWhen( true )] out IAspectClass? aspectClass );
}

internal static class AspectClassResolverExtensions
{
    public static IAspectClass GetAspectClass( this IAspectClassResolver resolver, Type aspectType )
    {
        if ( !resolver.TryGetAspectClass( aspectType, out var aspectClass ) )
        {
            throw new AssertionFailedException( $"Cannot find an IAspectClass for type '{aspectType}'." );
        }

        return aspectClass;
    }

    public static IAspectClass GetAspectClass( this IAspectClassResolver resolver, string fullName )
    {
        if ( !resolver.TryGetAspectClass( fullName, out var aspectClass ) )
        {
            throw new AssertionFailedException( $"Cannot find an IAspectClass for type '{fullName}'." );
        }

        return aspectClass;
    }
}