// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// To avoid confusion due to some identical type names, this file is concerned only with Roslyn types.
// Do not add usings for the namespaces Metalama.Framework.Code, Metalama.Framework.Engine.CodeModel or
// related namespaces.

using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Patterns.Observability.Implementation.DependencyAnalysis;

[CompileTime]
internal static class RoslynExtensions
{
    public static Accessibility GetEffectiveAccessibility( this ISymbol symbol )
    {
        if ( symbol.DeclaredAccessibility != Accessibility.NotApplicable )
        {
            return symbol.DeclaredAccessibility;
        }

        switch ( symbol.Kind )
        {
            case SymbolKind.Property:
            case SymbolKind.Method:
            case SymbolKind.Field:
            case SymbolKind.Event:
            case SymbolKind.NamedType when symbol.ContainingType != null:
                {
                    var t = symbol.ContainingType;

                    if ( t == null )
                    {
                        return Accessibility.NotApplicable;
                    }

                    return t.TypeKind switch
                    {
                        TypeKind.Class or TypeKind.Struct => Accessibility.Private,
                        TypeKind.Interface or TypeKind.Enum => Accessibility.Public,
                        _ => throw new NotSupportedException()
                    };
                }

            case SymbolKind.NamedType:
                return Accessibility.Internal;

            default:
                throw new NotSupportedException();
        }
    }

    public static bool IsOrInheritsFrom( this INamedTypeSymbol type, ITypeSymbol? candidateBaseType )
    {
        if ( type == null )
        {
            throw new ArgumentNullException( nameof(type) );
        }

        if ( candidateBaseType == null )
        {
            return false;
        }

        var baseType = type;

        while ( baseType != null )
        {
            if ( candidateBaseType.Equals( baseType ) )
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    public static ITypeSymbol GetElementaryType( this ITypeSymbol type )
    {
        while ( true )
        {
            var elementType = Get( type );

            if ( elementType == type )
            {
                return elementType;
            }

            type = elementType;
        }

        static ITypeSymbol Get( ITypeSymbol t )
        {
            if ( t.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T )
            {
                return ((INamedTypeSymbol) t).TypeArguments[0];
            }

            return t switch
            {
                IArrayTypeSymbol array => array.ElementType,
                IPointerTypeSymbol pointer => pointer.PointedAtType,
                _ => t
            };
        }
    }

    /// <summary>
    /// Gets the body of the property getter, if any.
    /// </summary>
    public static SyntaxNode? GetGetterBody( this PropertyDeclarationSyntax property )
    {
        if ( property.ExpressionBody != null )
        {
            return property.ExpressionBody;
        }

        if ( property.AccessorList == null )
        {
            return null;
        }

        // We are not using LINQ to work around a bug (#33676) with lambda expressions in compile-time code.
        foreach ( var accessor in property.AccessorList.Accessors )
        {
            if ( accessor.Keyword.IsKind( SyntaxKind.GetKeyword ) )
            {
                return (SyntaxNode?) accessor.ExpressionBody ?? accessor.Body;
            }
        }

        return null;
    }
}