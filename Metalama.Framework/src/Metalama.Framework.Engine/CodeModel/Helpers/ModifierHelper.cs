// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Metalama.Framework.Code.Accessibility;
using RefKind = Metalama.Framework.Code.RefKind;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

internal static class ModifierHelper
{
    public static SyntaxTokenList GetSyntaxModifierList( this IDeclaration declaration, ModifierCategories categories = ModifierCategories.All )
    {
        switch ( declaration.DeclarationKind )
        {
            case DeclarationKind.Method when declaration is IMethodImpl accessor && accessor.IsAccessor():
                return GetAccessorSyntaxModifierList( accessor, categories );

            case DeclarationKind.Method when declaration is IMethodImpl method:
                return GetMemberSyntaxModifierList( method, categories );

            case DeclarationKind.Constructor when declaration is IConstructorImpl constructor:
                return GetMemberSyntaxModifierList( constructor, categories );

            case DeclarationKind.Property when declaration is IPropertyImpl property:
                return GetMemberSyntaxModifierList( property, categories );

            case DeclarationKind.Indexer when declaration is IIndexerImpl indexer:
                return GetMemberSyntaxModifierList( indexer, categories );

            case DeclarationKind.Event when declaration is IEventImpl @event:
                return GetMemberSyntaxModifierList( @event, categories );

            case DeclarationKind.Parameter when declaration is IParameterImpl parameter:
                return GetParameterSyntaxModifierList( parameter );

            case DeclarationKind.Field when declaration is IFieldImpl field:
                return GetMemberSyntaxModifierList( field, categories );

            case DeclarationKind.NamedType when declaration is INamedTypeImpl namedType:
                return GetTypeSyntaxModifierList( namedType, categories );

            default:
                throw new AssertionFailedException( $"Unexpected declaration kind: {declaration.DeclarationKind}." );
        }
    }

    private static SyntaxTokenList GetAccessorSyntaxModifierList( IMethod accessor, ModifierCategories categories )
    {
        var methodGroup = (IMemberOrNamedType) accessor.ContainingDeclaration!;

        // TODO: Unify with ToRoslynAccessibility and some roslyn helper?
        var tokens = new List<SyntaxToken>();

        if ( (categories & ModifierCategories.Accessibility) != 0 )
        {
            if ( accessor.Accessibility != methodGroup.Accessibility )
            {
                AddAccessibilityTokens( accessor, tokens );
            }
        }

        return TokenList( tokens );
    }

    private static SyntaxTokenList GetMemberSyntaxModifierList( IMemberImpl member, ModifierCategories categories )
    {
        // TODO: Unify with ToRoslynAccessibility and some roslyn helper?
        var tokens = new List<SyntaxToken>();

        void AddToken( SyntaxKind syntaxKind )
        {
            tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( syntaxKind ) );
        }

        // Private void partial methods skip accessibility to make implementation non-mandatory.
        if ( (categories & ModifierCategories.Accessibility) != 0
             && member is not IMethod { IsPartial: true, Accessibility: Accessibility.Private, ReturnType.SpecialType: SpecialType.Void } )
        {
            AddAccessibilityTokens( member, tokens );
        }

        if ( (categories & ModifierCategories.Required) != 0 && member is IFieldOrProperty { IsRequired: true } )
        {
            AddToken( SyntaxKind.RequiredKeyword );
        }

        if ( member.IsStatic && (categories & ModifierCategories.Static) != 0 )
        {
            AddToken( SyntaxKind.StaticKeyword );
        }

        if ( member.IsPartial && (categories & ModifierCategories.Partial) != 0 )
        {
            AddToken( SyntaxKind.PartialKeyword );
        }

        if ( member.IsExtern && (categories & ModifierCategories.Extern) != 0 )
        {
            AddToken( SyntaxKind.ExternKeyword );
        }

        if ( (categories & ModifierCategories.Inheritance) != 0 )
        {
            if ( member.HasNewKeyword == true )
            {
                AddToken( SyntaxKind.NewKeyword );
            }

            if ( member.DeclaringType is { TypeKind: TypeKind.Interface } )
            {
                Invariant.Assert( !member.IsOverride );
                Invariant.Implies( member.IsAbstract, member.Accessibility is not Accessibility.Private );
                Invariant.Implies( member.IsVirtual, member.Accessibility is not Accessibility.Private );

                // Interface instance methods are automatically abstract or virtual depending on presence of the body.
                // Override keyword is not allowed in interfaces.
                if ( member.IsStatic )
                {
                    if ( member.IsAbstract )
                    {
                        AddToken( SyntaxKind.AbstractKeyword );
                    }
                    else if ( member.IsVirtual )
                    {
                        AddToken( SyntaxKind.VirtualKeyword );
                    }
                }
            }
            else
            {
                if ( member.IsOverride )
                {
                    AddToken( SyntaxKind.OverrideKeyword );
                }
                else if ( member.IsAbstract )
                {
                    AddToken( SyntaxKind.AbstractKeyword );
                }
                else if ( member.IsVirtual )
                {
                    AddToken( SyntaxKind.VirtualKeyword );
                }
            }

            if ( member.IsSealed )
            {
                AddToken( SyntaxKind.SealedKeyword );
            }
        }

        if ( (categories & ModifierCategories.ReadOnly) != 0
             && member.DeclarationKind is DeclarationKind.Method or DeclarationKind.Field
             && member is IMethod { IsReadOnly: true } or IField { Writeability: Writeability.ConstructorOnly } )
        {
            AddToken( SyntaxKind.ReadOnlyKeyword );
        }

        if ( (categories & ModifierCategories.Const) != 0
             && member.DeclarationKind == DeclarationKind.Field
             && member is IField { Writeability: Writeability.None } )
        {
            AddToken( SyntaxKind.ConstKeyword );
        }

        if ( (categories & ModifierCategories.Unsafe) != 0 && member.GetSymbol() is { } symbol && symbol.HasModifier( SyntaxKind.UnsafeKeyword ) == true )
        {
            AddToken( SyntaxKind.UnsafeKeyword );
        }

        if ( (categories & ModifierCategories.Volatile) != 0 && member.GetSymbol() is IFieldSymbol { IsVolatile: true } )
        {
            AddToken( SyntaxKind.VolatileKeyword );
        }

        if ( (categories & ModifierCategories.Async) != 0 && member.IsAsync )
        {
            AddToken( SyntaxKind.AsyncKeyword );
        }

        return TokenList( tokens );
    }

    private static SyntaxTokenList GetTypeSyntaxModifierList( INamedTypeImpl namedType, ModifierCategories categories )
    {
        var tokens = new List<SyntaxToken>();

        void AddToken( SyntaxKind syntaxKind )
        {
            tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( syntaxKind ) );
        }

        if ( (categories & ModifierCategories.Accessibility) != 0 )
        {
            AddAccessibilityTokens( namedType, tokens );
        }

        if ( namedType.IsStatic && (categories & ModifierCategories.Static) != 0 )
        {
            AddToken( SyntaxKind.StaticKeyword );
        }

        if ( (categories & ModifierCategories.Inheritance) != 0 )
        {
            if ( namedType.HasNewKeyword == true )
            {
                AddToken( SyntaxKind.NewKeyword );
            }

            if ( namedType.IsAbstract && namedType.TypeKind != TypeKind.Interface )
            {
                AddToken( SyntaxKind.AbstractKeyword );
            }

            if ( namedType.IsSealed )
            {
                AddToken( SyntaxKind.SealedKeyword );
            }
        }

        return TokenList( tokens );
    }

    private static void AddAccessibilityTokens( IMemberOrNamedType member, List<SyntaxToken> tokens )
    {
        void AddToken( SyntaxKind syntaxKind )
        {
            tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( syntaxKind ) );
        }

        // If the target is explicit interface implementation, skip accessibility modifiers.
        switch ( member.DeclarationKind )
        {
            case DeclarationKind.Method when member is IMethod method:
                if ( method.ExplicitInterfaceImplementations.Count > 0 )
                {
                    return;
                }

                break;

            case DeclarationKind.Property when member is IProperty property:
                if ( property.ExplicitInterfaceImplementations.Count > 0 )
                {
                    return;
                }

                break;

            case DeclarationKind.Event when member is IEvent @event:
                if ( @event.ExplicitInterfaceImplementations.Count > 0 )
                {
                    return;
                }

                break;
        }

        switch ( member.Accessibility )
        {
            case Accessibility.Private:
                AddToken( SyntaxKind.PrivateKeyword );

                break;

            case Accessibility.PrivateProtected:
                AddToken( SyntaxKind.PrivateKeyword );
                AddToken( SyntaxKind.ProtectedKeyword );

                break;

            case Accessibility.Protected:
                AddToken( SyntaxKind.ProtectedKeyword );

                break;

            case Accessibility.Internal:
                AddToken( SyntaxKind.InternalKeyword );

                break;

            case Accessibility.ProtectedInternal:
                AddToken( SyntaxKind.ProtectedKeyword );
                AddToken( SyntaxKind.InternalKeyword );

                break;

            case Accessibility.Public:
                if ( member.DeclaringType is not { TypeKind: TypeKind.Interface } )
                {
                    // Idiomatically, public accessor is skipped in interfaces.
                    AddToken( SyntaxKind.PublicKeyword );
                }

                break;
        }
    }

    private static SyntaxTokenList GetParameterSyntaxModifierList( IParameter parameter )
    {
        var tokens = new List<SyntaxToken>( 2 );

        void AddToken( SyntaxKind syntaxKind )
        {
            tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( syntaxKind ) );
        }

        if ( parameter.IsThis )
        {
            AddToken( SyntaxKind.ThisKeyword );
        }

        AddRefKindModifiers( parameter.RefKind, tokens );

        if ( parameter.IsParams )
        {
            AddToken( SyntaxKind.ParamsKeyword );
        }

        return TokenList( tokens );
    }

    /// <summary>
    /// Gets the syntax token list for a parameter RefKind (ref, in, out, ref readonly).
    /// </summary>
    internal static SyntaxTokenList GetRefKindModifiers( RefKind refKind )
    {
        var tokens = new List<SyntaxToken>( 2 );
        AddRefKindModifiers( refKind, tokens );

        return TokenList( tokens );
    }

    private static void AddRefKindModifiers( RefKind refKind, List<SyntaxToken> tokens )
    {
        void AddToken( SyntaxKind syntaxKind )
        {
            tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( syntaxKind ) );
        }

        switch ( refKind )
        {
            case RefKind.None:
                // Do nothing.
                break;

            case RefKind.In:
                AddToken( SyntaxKind.InKeyword );

                break;

            case RefKind.RefReadOnly:
                AddToken( SyntaxKind.RefKeyword );
                AddToken( SyntaxKind.ReadOnlyKeyword );

                break;

            case RefKind.Ref:
                AddToken( SyntaxKind.RefKeyword );

                break;

            case RefKind.Out:
                AddToken( SyntaxKind.OutKeyword );

                break;

            default:
                throw new AssertionFailedException( $"Unexpected parameter RefKind {refKind}." );
        }
    }
}