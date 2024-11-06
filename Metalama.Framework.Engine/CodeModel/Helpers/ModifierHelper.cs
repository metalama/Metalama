// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

namespace Metalama.Framework.Engine.CodeModel.Helpers;

internal static class ModifierHelper
{
    public static SyntaxTokenList GetSyntaxModifierList( this IDeclaration declaration, ModifierCategories categories = ModifierCategories.All )
    {
        // TODO Perf: switch by DeclarationKind.
        switch ( declaration )
        {
            case IMethodImpl accessor when accessor.IsAccessor():
                return GetAccessorSyntaxModifierList( accessor, categories );

            case IMethodImpl method:
                return GetMemberSyntaxModifierList( method, categories );

            case IConstructorImpl constructor:
                return GetMemberSyntaxModifierList( constructor, categories );

            case IPropertyImpl property:
                return GetMemberSyntaxModifierList( property, categories );

            case IIndexerImpl indexer:
                return GetMemberSyntaxModifierList( indexer, categories );

            case IEventImpl @event:
                return GetMemberSyntaxModifierList( @event, categories );

            case IParameterImpl parameter:
                return GetParameterSyntaxModifierList( parameter );

            case IFieldImpl field:
                return GetMemberSyntaxModifierList( field, categories );

            case INamedTypeImpl namedType:
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
             && member is not IMethod { IsPartial: true, Accessibility: Accessibility.Private, ReturnType: { SpecialType: Code.SpecialType.Void } } )
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

            if ( member.DeclaringType is { TypeKind: Code.TypeKind.Interface } )
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

        if ( (categories & ModifierCategories.ReadOnly) != 0 && member is IMethod { IsReadOnly: true } or
                IField { Writeability: Writeability.ConstructorOnly } )
        {
            AddToken( SyntaxKind.ReadOnlyKeyword );
        }

        if ( (categories & ModifierCategories.Const) != 0 && member is IField { Writeability: Writeability.None } )
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
            
            if ( namedType.IsAbstract && namedType.TypeKind != Code.TypeKind.Interface )
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
        switch ( member )
        {
            case IMethod method:
                if ( method.ExplicitInterfaceImplementations.Count > 0 )
                {
                    return;
                }

                break;

            case IProperty property:
                if ( property.ExplicitInterfaceImplementations.Count > 0 )
                {
                    return;
                }

                break;

            case IEvent @event:
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
                if (member.DeclaringType is not { TypeKind: Code.TypeKind.Interface })
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

        switch ( parameter.RefKind )
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
                throw new AssertionFailedException( $"Unexpected parameter RefKind {parameter.RefKind}." );
        }

        if ( parameter.IsParams )
        {
            AddToken( SyntaxKind.ParamsKeyword );
        }

        return TokenList( tokens );
    }
}