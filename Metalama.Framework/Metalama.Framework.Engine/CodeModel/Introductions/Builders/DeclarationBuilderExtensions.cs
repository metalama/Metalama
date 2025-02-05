// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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