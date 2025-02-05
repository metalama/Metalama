// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Aspects;

public static class AttributeHelper
{
    [return: NotNullIfNotNull( "name" )]
    internal static string? GetShortName( string? name )
    {
        if ( name == null )
        {
            return null;
        }

        Parse( name, out _, out var shortName );

        return shortName;
    }

    public static void Parse( string fullName, out string ns, out string shortName )
    {
        string typeName;

        var lastDot = fullName.LastIndexOf( '.' );

        if ( lastDot >= 0 )
        {
            ns = fullName.Substring( 0, lastDot );
            typeName = fullName.Substring( lastDot + 1 );
        }
        else
        {
            ns = "";
            typeName = fullName;
        }

        shortName = typeName.TrimSuffix( "Attribute" );
    }

    internal static bool IsValid( this AttributeData attributeData )
    {
        if ( attributeData.AttributeConstructor == null )
        {
            return false;
        }

        if ( attributeData.AttributeClass == null || attributeData.AttributeClass.TypeKind == TypeKind.Error )
        {
            return false;
        }

        foreach ( var argument in attributeData.ConstructorArguments )
        {
            if ( !IsTypedConstantValid( argument ) )
            {
                return false;
            }
        }

        foreach ( var namedArgument in attributeData.NamedArguments )
        {
            if ( !IsTypedConstantValid( namedArgument.Value ) )
            {
                return false;
            }
        }

        return true;

        static bool IsTypedConstantValid( TypedConstant typedConstant )
        {
            switch ( typedConstant.Kind )
            {
                case TypedConstantKind.Error:
                case TypedConstantKind.Type when typedConstant.Value is IErrorTypeSymbol:

                    return false;

                case TypedConstantKind.Array when !typedConstant.IsNull:
                    {
                        foreach ( var item in typedConstant.Values )
                        {
                            if ( !IsTypedConstantValid( item ) )
                            {
                                return false;
                            }
                        }
                    }

                    return true;

                default:
                    return true;
            }
        }
    }
}