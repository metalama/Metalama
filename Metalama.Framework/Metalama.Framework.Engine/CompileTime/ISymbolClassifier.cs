// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        TemplatingScope GetTemplatingScope( ISymbol symbol );

        bool IsTemplateOnly( ISymbol symbol );

        void ReportScopeError( SyntaxNode node, ISymbol symbol, IDiagnosticAdder diagnosticAdder );
    }
}