// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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