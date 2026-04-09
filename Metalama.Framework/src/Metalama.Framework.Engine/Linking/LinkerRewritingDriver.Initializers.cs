// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Metalama.Framework.Engine.Linking
{
    internal sealed partial class LinkerRewritingDriver
    {
        /// <summary>
        /// Applies <see cref="OnInitializedCallSiteSubstitution"/> instances registered for a field,
        /// event-field or property symbol to the initializer of its declaration. Returns the
        /// initializer unchanged when no substitutions are registered.
        /// </summary>
        /// <remarks>
        /// Uses <c>SyntaxNode.ReplaceNodes</c>, which applies its callback bottom-up, so nested
        /// object-creation expressions are wrapped correctly (e.g.
        /// <c>new X { Y = new Y { Z = new Z() } }</c> becomes
        /// <c>WithInitialize(new X { Y = WithInitialize(new Y { Z = WithInitialize(new Z()) }) })</c>).
        /// </remarks>
        private EqualsValueClauseSyntax RewriteInitializer(
            ISymbol memberSymbol,
            EqualsValueClauseSyntax initializer,
            SyntaxGenerationContext syntaxGenerationContext )
        {
            if ( !this.AnalysisRegistry.TryGetInitializerSubstitutions( memberSymbol, out var substitutions ) )
            {
                return initializer;
            }

            var byOriginalNode = substitutions.ToDictionary( s => s.ReplacedNode );
            var substitutionContext = new SubstitutionContext( this, syntaxGenerationContext );

            var rewritten = initializer.ReplaceNodes(
                initializer.DescendantNodes().Where( byOriginalNode.ContainsKey ),
                ( original, rewrittenWithChildren ) =>
                    byOriginalNode[original].Substitute( rewrittenWithChildren, substitutionContext )! );

            return rewritten;
        }
    }
}
