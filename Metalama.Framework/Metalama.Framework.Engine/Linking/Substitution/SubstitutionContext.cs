// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking.Substitution
{
    internal sealed class SubstitutionContext
    {
        private readonly Lazy<IReadOnlyDictionary<SyntaxNode, SyntaxNodeSubstitution>?> _substitutionDictionary;

        public LinkerRewritingDriver RewritingDriver { get; }

        public SyntaxGenerationContext SyntaxGenerationContext { get; }

        public SubstitutionContext(
            LinkerRewritingDriver rewritingDriver,
            SyntaxGenerationContext syntaxGenerationContext,
            InliningContextIdentifier inliningContextId )
        {
            this.RewritingDriver = rewritingDriver;
            this.SyntaxGenerationContext = syntaxGenerationContext;

            this._substitutionDictionary =
                new Lazy<IReadOnlyDictionary<SyntaxNode, SyntaxNodeSubstitution>?>( () => this.RewritingDriver.GetSubstitutions( inliningContextId ) );
        }

        internal SubstitutionContext WithInliningContext( InliningContextIdentifier inliningContextId )
        {
            return new SubstitutionContext( this.RewritingDriver, this.SyntaxGenerationContext, inliningContextId );
        }

        public IReadOnlyDictionary<SyntaxNode, SyntaxNodeSubstitution>? GetSubstitutions()
        {
            return this._substitutionDictionary.Value;
        }
    }
}