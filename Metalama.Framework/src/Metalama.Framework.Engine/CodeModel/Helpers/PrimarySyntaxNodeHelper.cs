// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Helpers
{
    public static class PrimarySyntaxNodeHelper
    {
        public static SyntaxNode? GetPrimaryDeclarationSyntax( this IDeclaration declaration )
            => declaration switch
            {
                ISymbolBasedCompilationElement symbolBased => symbolBased.Symbol.GetPrimaryDeclarationSyntax(),
                _ => null
            };

        private static ISymbol? GetClosestSymbol( this IDeclaration declaration )
        {
            for ( var d = declaration; d != null && d.DeclarationKind != DeclarationKind.Namespace; d = d.ContainingDeclaration )
            {
                if ( d is ISymbolBasedCompilationElement symbolBased )
                {
                    return symbolBased.Symbol;
                }
            }

            return null;
        }

        internal static SyntaxNode? GetClosestPrimaryDeclarationSyntax( this IDeclaration declaration )
        {
            // Then find the first symbol with syntax.
            for ( var symbol = GetClosestSymbol( declaration ); symbol != null && symbol.Kind != SymbolKind.Namespace; symbol = symbol.ContainingSymbol )
            {
                var syntax = symbol.GetPrimaryDeclarationSyntax();

                if ( syntax != null )
                {
                    return syntax;
                }
            }

            return null;
        }

        internal static SyntaxNode? GetClosestPrimaryDeclarationSyntax( this ISymbol symbol )
        {
            for ( var s = symbol; s != null && s.Kind != SymbolKind.Namespace; s = s.ContainingSymbol )
            {
                var syntax = s.GetPrimaryDeclarationSyntax();

                if ( syntax != null )
                {
                    return syntax;
                }
            }

            return null;
        }

        internal static SyntaxNode? GetPrimaryDeclarationSyntax( this IFullRef declaration )
            => declaration.GetClosestContainingSymbol().GetPrimaryDeclarationSyntax();

        public static SyntaxTree? GetPrimarySyntaxTree( this IDeclaration declaration ) => ((IDeclarationImpl) declaration).PrimarySyntaxTree;
    }
}