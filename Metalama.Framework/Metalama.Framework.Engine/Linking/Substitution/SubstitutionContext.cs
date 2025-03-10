// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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