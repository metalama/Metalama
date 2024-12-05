// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
            { DeclarationKind.NamedType, 5 }
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
            if ( declaration is IMethod )
            {
                var signatureComparison = StructuralSymbolComparer.NameOblivious.Compare( declaration.GetSymbol(), otherDeclaration.GetSymbol() );

                if ( signatureComparison != 0 )
                {
                    return signatureComparison;
                }
            }

            // Order by accessibility.
            if ( declaration is IMemberOrNamedType memberOrNamedType && otherDeclaration is IMemberOrNamedType otherMemberOrNamedType )
            {
                var accessibilityComparison =
                    GetAccessibilityOrder( memberOrNamedType.Accessibility ).CompareTo( GetAccessibilityOrder( otherMemberOrNamedType.Accessibility ) );

                if ( accessibilityComparison != 0 )
                {
                    return accessibilityComparison;
                }
            }

            // Order by implemented interface.
            if ( declaration is IMember declarationMember && otherDeclaration is IMember otherDeclarationMember )
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
                    // Order by aspect layer.
                    var aspectLayerComparison = x.Transformation.OrderWithinPipeline.CompareTo( y.Transformation.OrderWithinPipeline );

                    if ( aspectLayerComparison != 0 )
                    {
                        return aspectLayerComparison;
                    }

                    // Order by aspect instance in the current type.
                    var aspectInstanceOrderComparison =
                        x.Transformation.OrderWithinPipelineStepAndType.CompareTo( y.Transformation.OrderWithinPipelineStepAndType );

                    if ( aspectInstanceOrderComparison != 0 )
                    {
                        return aspectInstanceOrderComparison;
                    }

                    // Order by adding order within the aspect instance.
                    var adviceOrderComparison =
                        x.Transformation.OrderWithinPipelineStepAndTypeAndAspectInstance.CompareTo(
                            y.Transformation.OrderWithinPipelineStepAndTypeAndAspectInstance );

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