// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking
{
    /// <summary>
    /// Maps diagnostic locations from the intermediate compilation back to the source compilation.
    /// The intermediate compilation's SyntaxTrees may have been modified (e.g., by adding 'partial' keyword
    /// or injecting members), causing line numbers to differ from the user's source code.
    /// </summary>
    internal static class LinkerDiagnosticMapper
    {
        /// <summary>
        /// Gets the diagnostic location of a symbol in the source compilation instead of the intermediate compilation.
        /// Returns <c>null</c> if the symbol cannot be found in the source compilation, in which case the caller
        /// should fall back to the intermediate compilation's location.
        /// </summary>
        public static Location? GetSourceLocation( ISymbol intermediateSymbol, CompilationContext sourceCompilationContext )
        {
            var sourceSymbol = sourceCompilationContext.SymbolTranslator.Translate( intermediateSymbol, allowMultiple: true );

            return sourceSymbol?.GetDiagnosticLocation();
        }
    }
}
