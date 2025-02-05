// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

internal static class GenericContextHelper
{
    internal static bool IsCanonicalGenericTypeInstance( INamedTypeSymbol namedTypeSymbol )
    {
        if ( namedTypeSymbol.IsDefinitionSafe() )
        {
            return true;
        }
        else
        {
            for ( var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++ )
            {
                if ( !namedTypeSymbol.TypeArguments[i].OriginalDefinition.Equals( namedTypeSymbol.TypeParameters[i].OriginalDefinition ) )
                {
                    return false;
                }
            }

            if ( namedTypeSymbol.ContainingType != null && !IsCanonicalGenericTypeInstance( namedTypeSymbol.ContainingType ) )
            {
                return false;
            }

            return true;
        }
    }

    internal static bool IsCanonicalGenericMethodInstance( IMethodSymbol methodSymbol )
    {
        if ( methodSymbol.IsDefinitionSafe() )
        {
            return true;
        }
        else
        {
            for ( var i = 0; i < methodSymbol.TypeArguments.Length; i++ )
            {
                if ( methodSymbol.TypeArguments[i] is not ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameterSymbol
                     || typeParameterSymbol.Ordinal != i )
                {
                    return false;
                }
            }

            if ( !IsCanonicalGenericTypeInstance( methodSymbol.ContainingType ) )
            {
                return false;
            }

            return true;
        }
    }
}