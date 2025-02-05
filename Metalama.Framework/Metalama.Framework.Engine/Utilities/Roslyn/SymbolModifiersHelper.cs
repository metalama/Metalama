// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Utilities.Roslyn
{
    internal static class SymbolModifiersHelper
    {
        public static SyntaxTokenList GetSyntaxModifierList( this ISymbol declaration, ModifierCategories categories = ModifierCategories.All )
        {
            switch ( declaration )
            {
                case IMethodSymbol accessor when accessor.IsAccessor():
                    return GetAccessorSyntaxModifierList( accessor, categories );

                case IMethodSymbol method:
                    return GetMemberSyntaxModifierList( method, categories );

                case IPropertySymbol property:
                    return GetMemberSyntaxModifierList( property, categories );

                case IEventSymbol @event:
                    return GetMemberSyntaxModifierList( @event, categories );

                case IParameterSymbol parameter:
                    return GetParameterSyntaxModifierList( parameter );

                case IFieldSymbol field:
                    return GetMemberSyntaxModifierList( field, categories );

                default:
                    throw new AssertionFailedException( $"Unexpected declaration: '{declaration}'." );
            }
        }

        private static SyntaxTokenList GetAccessorSyntaxModifierList( IMethodSymbol accessor, ModifierCategories categories )
        {
            var methodGroup = accessor.AssociatedSymbol!;

            // TODO: Unify with ToRoslynAccessibility and some roslyn helper?
            var tokens = new List<SyntaxToken>();

            if ( (categories & ModifierCategories.Accessibility) != 0 )
            {
                if ( accessor.DeclaredAccessibility != methodGroup.DeclaredAccessibility )
                {
                    AddAccessibilityTokens( accessor, tokens );
                }
            }

            return TokenList( tokens );
        }

        private static SyntaxTokenList GetMemberSyntaxModifierList( ISymbol member, ModifierCategories categories )
        {
            // TODO: Unify with ToRoslynAccessibility and some roslyn helper?
            var tokens = new List<SyntaxToken>();

            if ( (categories & ModifierCategories.Accessibility) != 0 )
            {
                AddAccessibilityTokens( member, tokens );
            }

            if ( member.IsStatic && (categories & ModifierCategories.Static) != 0 )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.StaticKeyword ) );
            }

            if ( (categories & ModifierCategories.Inheritance) != 0 )
            {
                if ( member.HasModifier( SyntaxKind.NewKeyword ) == true )
                {
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NewKeyword ) );
                }

                // The following modifiers are exclusive in C# but not in the symbol model.
                if ( member.IsOverride )
                {
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.OverrideKeyword ) );
                }
                else if ( member.IsAbstract )
                {
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.AbstractKeyword ) );
                }
                else if ( member.IsVirtual )
                {
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.VirtualKeyword ) );
                }

                if ( member.IsSealed )
                {
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.SealedKeyword ) );
                }
            }

            if ( (categories & ModifierCategories.ReadOnly) != 0 && member is IMethodSymbol { IsReadOnly: true } or IFieldSymbol { IsReadOnly: true } )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ReadOnlyKeyword ) );
            }

            if ( (categories & ModifierCategories.Const) != 0 && member is IFieldSymbol { IsConst: true } )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ConstKeyword ) );
            }

            if ( (categories & ModifierCategories.Unsafe) != 0 && member.HasModifier( SyntaxKind.UnsafeKeyword ) == true )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.UnsafeKeyword ) );
            }

            if ( (categories & ModifierCategories.Volatile) != 0 && member is IFieldSymbol { IsVolatile: true } )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.VolatileKeyword ) );
            }

            if ( (categories & ModifierCategories.Async) != 0 && member is IMethodSymbol { IsAsync: true } )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.AsyncKeyword ) );
            }

            return TokenList( tokens );
        }

        private static void AddAccessibilityTokens( ISymbol member, List<SyntaxToken> tokens )
        {
            // If the target is explicit interface implementation, skip accessibility modifiers.
            switch ( member )
            {
                case IMethodSymbol method:
                    if ( method.ExplicitInterfaceImplementations.Length > 0 )
                    {
                        return;
                    }

                    break;

                case IPropertySymbol property:
                    if ( property.ExplicitInterfaceImplementations.Length > 0 )
                    {
                        return;
                    }

                    break;

                case IEventSymbol @event:
                    if ( @event.ExplicitInterfaceImplementations.Length > 0 )
                    {
                        return;
                    }

                    break;
            }

            switch ( member.DeclaredAccessibility )
            {
                case Accessibility.Private:
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );

                    break;

                case Accessibility.ProtectedAndInternal:
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PrivateKeyword ) );
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ProtectedKeyword ) );

                    break;

                case Accessibility.Protected:
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ProtectedKeyword ) );

                    break;

                case Accessibility.Internal:
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InternalKeyword ) );

                    break;

                case Accessibility.ProtectedOrInternal:
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.ProtectedKeyword ) );
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InternalKeyword ) );

                    break;

                case Accessibility.Public:
                    tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.PublicKeyword ) );

                    break;
            }
        }

        private static SyntaxTokenList GetParameterSyntaxModifierList( IParameterSymbol parameter )
        {
            var tokens = new List<SyntaxToken>();

            if ( parameter.RefKind == RefKind.In )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.InKeyword ) );
            }

            if ( parameter.RefKind == RefKind.Ref )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.RefKeyword ) );
            }

            if ( parameter.RefKind == RefKind.Out )
            {
                tokens.Add( SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.OutKeyword ) );
            }

            return TokenList( tokens );
        }
    }
}