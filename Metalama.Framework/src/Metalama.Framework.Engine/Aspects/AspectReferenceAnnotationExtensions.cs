// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Provides extension methods for handling of aspect reference annotations.
    /// </summary>
    [UsedImplicitly] // Used by AspectWorkbench but JB tool does not see it.
    public static class AspectReferenceAnnotationExtensions
    {
        [UsedImplicitly]
        public const string AnnotationKind = "MetalamaAspectReference";

        /// <summary>
        /// Gets a specification of aspect reference if it is present on the syntax node.
        /// </summary>
        /// <param name="node">Syntax node.</param>
        /// <param name="specification">Specification of the aspect reference.</param>
        /// <returns></returns>
        internal static bool TryGetAspectReference( this SyntaxNode node, out AspectReferenceSpecification specification )
        {
            var annotationValue = node.GetAnnotations( AnnotationKind ).SingleOrDefault()?.Data;

            if ( annotationValue == null )
            {
                specification = default;

                return false;
            }
            else
            {
                specification = AspectReferenceSpecification.FromString( annotationValue );

                return true;
            }
        }

        /// <summary>
        /// Returns the current node with an annotation indicating how the aspect-generated code references a declaration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="annotation"></param>
        /// <returns>Annotated syntax node.</returns>
        internal static T WithAspectReferenceAnnotation<T>( this T node, in AspectReferenceSpecification annotation )
            where T : SyntaxNode
        {
            return node.WithAdditionalAnnotations( new SyntaxAnnotation( AnnotationKind, annotation.ToString() ) );
        }

        /// <summary>
        /// Returns the current node with an annotation indicating how the aspect-generated code references a declaration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <param name="aspectLayerId">Aspect layer which created the syntax node.</param>
        /// <param name="order">Version of the target semantic in relation to the aspect layer.</param>
        /// <param name="targetKind">Target kind. For example self or property get accessor.</param>
        /// <param name="flags">Flags.</param>
        /// <returns>Annotated syntax node.</returns>
        internal static T WithAspectReferenceAnnotation<T>(
            this T node,
            AspectLayerId aspectLayerId,
            AspectReferenceOrder order,
            AspectReferenceTargetKind targetKind = AspectReferenceTargetKind.Self,
            AspectReferenceFlags flags = AspectReferenceFlags.None,
            string? targetDeclarationId = null )
            where T : SyntaxNode
        {
            return node.WithAspectReferenceAnnotation( new AspectReferenceSpecification( aspectLayerId, order, targetKind, flags, targetDeclarationId ) );
        }

        /// <summary>
        /// Gets the documentation comment ID for a Metalama declaration, or <c>null</c> if the declaration is not symbol-backed
        /// or if the declaration requires complex resolution by <see cref="Linking.AspectReferenceResolver"/> (interface members,
        /// explicit interface implementations, overrides). Only returns an ID when the symbol will take the simple
        /// resolution path in the linker.
        /// </summary>
        internal static string? GetTargetDeclarationId( ICompilationElement declaration )
        {
            // Skip declarations that require complex ResolveTarget handling:
            // - Interface members: ResolveTarget calls FindImplementationForInterfaceMember.
            // - Explicit interface implementations: ResolveTarget handles these specially.
            // - Members whose symbol requires generic context mapping.
            if ( declaration is IMember member )
            {
                if ( member.DeclaringType.TypeKind == TypeKind.Interface
                     || member.IsExplicitInterfaceImplementation )
                {
                    return null;
                }
            }

            var symbol = declaration.GetSymbol();

            if ( symbol == null )
            {
                return null;
            }

            // Skip overrides because GetSymbolInfo resolves to the derived type's version of the symbol,
            // which differs from the declaration ID's resolution.
            if ( symbol is IMethodSymbol { IsOverride: true } or IPropertySymbol { IsOverride: true } or IEventSymbol { IsOverride: true } )
            {
                return null;
            }

            return DocumentationCommentId.CreateDeclarationId( symbol );
        }
    }
}