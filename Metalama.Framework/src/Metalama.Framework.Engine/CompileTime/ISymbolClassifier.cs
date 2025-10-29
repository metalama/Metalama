// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Determines the kind of symbol: template, <see cref="TemplatingScope.CompileTimeOnly"/>,
    /// <see cref="TemplatingScope.RunTimeOnly"/>.
    /// </summary>
    internal interface ISymbolClassifier
    {
        TemplateInfo GetTemplateInfo( ISymbol symbol );

        /// <summary>
        /// Gets the scope of a symbol in the context of a template.
        /// </summary>
        TemplatingScope GetTemplatingScope( ISymbol symbol, SymbolClassificationContext context = SymbolClassificationContext.Default );

        TemplatingScopeAndRule GetTemplatingScopeAndRule( ISymbol symbol, SymbolClassificationContext context = SymbolClassificationContext.Default );

        bool IsTemplateOnly( ISymbol symbol );

        void ReportScopeError( SyntaxNode node, ISymbol symbol, IDiagnosticAdder diagnosticAdder );
    }

    /// <summary>
    /// An enumeration of contexts in which <see cref="ISymbolClassifier"/> is invoked.
    /// </summary>
    internal enum SymbolClassificationContext
    {
        /// <summary>
        /// Any other context than run-time-only code.
        /// </summary>
        Default,

        /// <summary>
        /// Run-time-only code.
        /// </summary>
        RunTimeOnly
    }
}