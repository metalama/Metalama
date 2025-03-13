// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

internal static class DeclarationBuilderExtensions
{
    public static T AssertFrozen<T>( this T declarationBuilder )
        where T : DeclarationBuilder
    {
#if DEBUG
        if ( !declarationBuilder.IsFrozen )
        {
            throw new AssertionFailedException( $"The {declarationBuilder.GetType().Name} was expected to be frozen." );
        }

        return declarationBuilder;
#else
return declarationBuilder;
#endif
    }
}