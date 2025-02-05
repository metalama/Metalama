// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

internal static class SymbolExtensions
{
    public static AspectLinkerDeclarationFlags GetDeclarationFlags( this ISymbol symbol )
    {
        // TODO: Partials?
        var declaration = symbol.GetPrimaryDeclarationSyntax();

        switch ( declaration )
        {
            case MemberDeclarationSyntax memberDeclaration:
                return memberDeclaration.GetLinkerDeclarationFlags();

            case VariableDeclaratorSyntax variableDeclarator:
                return ((MemberDeclarationSyntax?) variableDeclarator.Parent?.Parent).AssertNotNull().GetLinkerDeclarationFlags();

            case ParameterSyntax { Parent.Parent: RecordDeclarationSyntax }:
                return default;

            case AccessorDeclarationSyntax accessorDeclaration:
                return accessorDeclaration.Parent.AssertNotNull().GetLinkerDeclarationFlags();

            case ArrowExpressionClauseSyntax:
                // We cannot have flags on getter of expression-bodied property.
                return default;

            case null:
                return default;

            default:
                throw new AssertionFailedException( $"Unexpected declaration syntax for '{symbol}': {declaration}" );
        }
    }

    public static bool IsExplicitInterfaceEventField( this ISymbol symbol )
    {
        if ( symbol is IEventSymbol eventSymbol )
        {
            var declaration = eventSymbol.GetPrimaryDeclarationSyntax();

            if ( declaration != null && declaration.GetLinkerDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.EventField ) )
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsCallerMemberNameParameter( this IParameterSymbol parameter )
        => parameter.GetAttributes()
            .Any( a => a.AttributeConstructor.AssertNotNull().ContainingType.GetFullName() == "System.Runtime.CompilerServices.CallerMemberNameAttribute" );

    /// <summary>
    /// Gets a symbol the "new" symbol is hiding.
    /// </summary>
    /// <param name="symbol">The hiding symbol.</param>
    /// <param name="hiddenSymbol">The hidden symbol.</param>
    /// <returns>Hidden symbol or null.</returns>
    public static bool TryGetHiddenSymbol( this ISymbol symbol, Compilation compilation, [NotNullWhen( true )] out ISymbol? hiddenSymbol )
    {
        if ( symbol is not (IMethodSymbol or IEventSymbol or IPropertySymbol) )
        {
            // Types never hide anything.
            hiddenSymbol = null;

            return false;
        }

        var currentType = symbol.ContainingType.BaseType;

        if ( symbol.IsOverride )
        {
            // Override symbol never hides anything.
            hiddenSymbol = null;

            return false;
        }

        while ( currentType != null )
        {
            var matchingSymbol = currentType.GetMembers()
                .SingleOrDefault(
                    member => member.IsVisibleTo( compilation, symbol )
                              && SignatureEquals( symbol, member ) );

            if ( matchingSymbol != null )
            {
                hiddenSymbol = matchingSymbol;

                return true;
            }

            currentType = currentType.BaseType;
        }

        hiddenSymbol = null;

        return false;

        static bool SignatureEquals( ISymbol localMember, ISymbol baseMember )
        {
            switch (localMember, baseMember)
            {
                case (IPropertySymbol property, IFieldSymbol field):
                    // Promoted field that hides a base field.
                    if ( StringComparer.Ordinal.Equals( property.Name, field.Name ) )
                    {
                        return true;
                    }

                    goto default;

                default:
                    return SignatureTypeComparer.Instance.Equals( localMember, baseMember );
            }
        }
    }

    public static ISymbol GetSingleMemberIncludingBase( this INamedTypeSymbol type, string name, Func<ISymbol, bool> condition )
    {
        var member = type.GetMembers( name ).SingleOrDefault( condition );

        return member ?? type.BaseType.AssertNotNull().GetSingleMemberIncludingBase( name, condition );
    }

    /// <summary>
    /// For most methods, directly returns their kind. For explicitly implemented methods, returns the kind of the implemented method.
    /// </summary>
    public static MethodKind GetImplementedMethodKind( this IMethodSymbol method )
        => method.MethodKind != MethodKind.ExplicitInterfaceImplementation
            ? method.MethodKind
            : method.ExplicitInterfaceImplementations.Single().MethodKind;
}