// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal static class FieldHelper
{
    public static IProperty? GetOverridingProperty( IField field )
    {
        if ( !field.GenericContext.IsEmptyOrIdentity )
        {
            var propertyDefinition = GetOverridingPropertyDefinition( field.Definition );

            if ( propertyDefinition == null )
            {
                return null;
            }
            else
            {
                return propertyDefinition.ForTypeInstance( field.DeclaringType );
            }
        }
        else
        {
            return GetOverridingPropertyDefinition( field );
        }
    }

    private static IProperty? GetOverridingPropertyDefinition( IField field )
    {
        Invariant.Assert( field.Definition == field );
        var compilation = field.GetCompilationModel();

        if ( compilation.TryGetRedirectedDeclaration( field.ToRef(), out var builderData ) )
        {
            return compilation.Factory.GetProperty( (PropertyBuilderData) builderData );
        }
        else
        {
            return null;
        }
    }
}