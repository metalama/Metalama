// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Comparers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerInjectionStep
{
    private sealed class InjectedMemberComparer : IComparer<InjectedMember>
    {
        private static readonly ImmutableDictionary<DeclarationKind, int> _orderedDeclarationKinds = new Dictionary<DeclarationKind, int>()
        {
            { DeclarationKind.Field, 0 },
            { DeclarationKind.Constructor, 1 },
            { DeclarationKind.Property, 2 },
            { DeclarationKind.Method, 3 },
            { DeclarationKind.Event, 4 },
            { DeclarationKind.NamedType, 5 },
            { DeclarationKind.ExtensionBlock, 6 }
        }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<Accessibility, int> _orderedAccessibilities = new Dictionary<Accessibility, int>()
        {
            { Accessibility.Public, 0 },
            { Accessibility.Protected, 1 },
            { Accessibility.ProtectedInternal, 2 },
            { Accessibility.Internal, 3 },
            { Accessibility.PrivateProtected, 4 },
            { Accessibility.Private, 5 }
        }.ToImmutableDictionary();

        public int Compare( InjectedMember? x, InjectedMember? y )
        {
            if ( x == y )
            {
                return 0;
            }

            if ( x == null && y == null )
            {
                return 0;
            }
            else if ( x == null )
            {
                return 1;
            }
            else if ( y == null )
            {
                return -1;
            }

            // Order by kind.
            var kindComparison = GetKindOrder( x.Kind ).CompareTo( GetKindOrder( y.Kind ) );

            if ( kindComparison != 0 )
            {
                return kindComparison;
            }

            // Order by name.
            var nameComparison = string.CompareOrdinal( x.Declaration.Name, y.Declaration.Name );

            if ( nameComparison != 0 )
            {
                return nameComparison;
            }

            var declaration = GetDeclaration( x );
            var otherDeclaration = GetDeclaration( y );

            // Order by signature.
            if ( declaration.DeclarationKind == DeclarationKind.Method )
            {
                var signatureComparison = StructuralSymbolComparer.NameOblivious.Compare( declaration.GetSymbol(), otherDeclaration.GetSymbol() );

                if ( signatureComparison != 0 )
                {
                    return signatureComparison;
                }
            }

            // Order by accessibility.
            if ( declaration.DeclarationKind is DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Field or DeclarationKind.Event or DeclarationKind.Indexer or DeclarationKind.Constructor or DeclarationKind.NamedType or DeclarationKind.ExtensionBlock && declaration is IMemberOrNamedType memberOrNamedType
                 && otherDeclaration.DeclarationKind is DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Field or DeclarationKind.Event or DeclarationKind.Indexer or DeclarationKind.Constructor or DeclarationKind.NamedType or DeclarationKind.ExtensionBlock && otherDeclaration is IMemberOrNamedType otherMemberOrNamedType )
            {
                var accessibilityComparison =
                    GetAccessibilityOrder( memberOrNamedType.Accessibility ).CompareTo( GetAccessibilityOrder( otherMemberOrNamedType.Accessibility ) );

                if ( accessibilityComparison != 0 )
                {
                    return accessibilityComparison;
                }
            }

            // Order by implemented interface.
            if ( declaration.DeclarationKind is DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Field or DeclarationKind.Event or DeclarationKind.Indexer or DeclarationKind.Constructor && declaration is IMember declarationMember
                 && otherDeclaration.DeclarationKind is DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Field or DeclarationKind.Event or DeclarationKind.Indexer or DeclarationKind.Constructor && otherDeclaration is IMember otherDeclarationMember )
            {
                var isExplicitInterfaceImplementationComparison =
                    declarationMember.IsExplicitInterfaceImplementation.CompareTo( otherDeclarationMember.IsExplicitInterfaceImplementation );

                if ( isExplicitInterfaceImplementationComparison != 0 )
                {
                    return -isExplicitInterfaceImplementationComparison;
                }
                else if ( declarationMember.IsExplicitInterfaceImplementation )
                {
                    var interfaceComparison = string.Compare(
                        declarationMember.GetExplicitInterfaceImplementation().DeclaringType.FullName,
                        otherDeclarationMember.GetExplicitInterfaceImplementation().DeclaringType.FullName,
                        StringComparison.Ordinal );

                    if ( interfaceComparison != 0 )
                    {
                        return interfaceComparison;
                    }
                }
            }

            // Order by type of introduction.
            var typeComparison = GetTransformationTypeOrder( x.Transformation ).CompareTo( GetTransformationTypeOrder( y.Transformation ) );

            if ( typeComparison != 0 )
            {
                return typeComparison;
            }

            switch (x.Transformation, y.Transformation)
            {
                case (null, null):
                    return 0;

                case (null, _):
                    return -1;

                case (_, null):
                    return 1;

                case (not null, not null):
                    // Order by adding order within the pipeline, then type, then aspect instance.
                    var adviceOrderComparison =
                        x.Transformation.AdviceOrderingIndices.CompareTo( y.Transformation.AdviceOrderingIndices );

                    if ( adviceOrderComparison != 0 )
                    {
                        return adviceOrderComparison;
                    }

                    // Order by semantic.
                    var semanticComparison = GetSemanticOrder( x.Semantic ).CompareTo( GetSemanticOrder( y.Semantic ) );

                    if ( semanticComparison != 0 )
                    {
                        return semanticComparison;
                    }

                    // Order replaced declarations within the same layer.
                    if ( x.Transformation is IReplaceMemberTransformation { ReplacedMember: IIntroducedRef builderRefX }
                         && builderRefX.BuilderData.Equals( y.BuilderData ) )
                    {
                        return 1;
                    }

                    if ( y.Transformation is IReplaceMemberTransformation { ReplacedMember: IIntroducedRef builderRefY }
                         && builderRefY.BuilderData.Equals( x.BuilderData ) )
                    {
                        return -1;
                    }

                    break;
            }

            // TODO: At this point, all should be sorted, but mocks are not setting the order properties.
            // throw new AssertionFailedException( $"'{x}' and '{y}' are not strongly ordered" );
            return StringComparer.Ordinal.Compare( x.Syntax.ToString(), y.Syntax.ToString() );
        }

        private static int GetKindOrder( DeclarationKind kind ) => _orderedDeclarationKinds.TryGetValue( kind, out var order ) ? order : 10;

        private static int GetAccessibilityOrder( Accessibility accessibility )
            => _orderedAccessibilities.TryGetValue( accessibility, out var order ) ? order : 10;

        private static int GetTransformationTypeOrder( ITransformation? transformation ) => transformation is IOverrideDeclarationTransformation ? 0 : 1;

        private static int GetSemanticOrder( InjectedMemberSemantic semantic ) => semantic != InjectedMemberSemantic.InitializerMethod ? 0 : 1;

        private static INamedDeclaration GetDeclaration( InjectedMember injectedMember )
        {
            var declaration = injectedMember.Declaration;

            if ( injectedMember.Transformation is IOverrideDeclarationTransformation overridden )
            {
                declaration = overridden.OverriddenDeclaration;
            }

            if ( declaration == null )
            {
                throw new AssertionFailedException( "Don't know how to sort." );
            }

            return declaration.As<INamedDeclaration>().Definition;
        }
    }
}