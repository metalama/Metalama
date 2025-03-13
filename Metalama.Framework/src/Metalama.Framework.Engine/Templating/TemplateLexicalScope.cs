// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Generates unique name in a lexical scope. 
    /// </summary>
    /// <remarks>
    /// The implementation is intentionally single-threaded because using it in a concurrent condition would cause
    /// the generation of non-deterministic symbol names.
    /// </remarks>
    internal sealed class TemplateLexicalScope
    {
        private readonly ImmutableHashSet<string> _sourceSymbols;
        private readonly HashSet<string> _newSymbols = new();

        public TemplateLexicalScope( ImmutableHashSet<string> sourceSymbols )
        {
            this._sourceSymbols = sourceSymbols;
        }

        public string GetUniqueIdentifier( string hint )
        {
            if ( SyntaxFacts.GetKeywordKind( hint ) == SyntaxKind.None && !this._sourceSymbols.Contains( hint ) && this._newSymbols.Add( hint ) )
            {
                return hint;
            }

            for ( var i = 1; /* Intentionally empty */; i++ )
            {
                var name = hint + "_" + i;

                if ( !this._sourceSymbols.Contains( name ) && this._newSymbols.Add( name ) )
                {
                    return name;
                }
            }
        }
    }
}