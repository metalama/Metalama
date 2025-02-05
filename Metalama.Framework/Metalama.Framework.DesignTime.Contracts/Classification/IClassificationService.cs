// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Metalama.Framework.DesignTime.Contracts.Classification
{
    /// <summary>
    /// Exposes a method <see cref="GetClassifiedTextSpans"/> that returns a set of <see cref="DesignTimeClassifiedTextSpan"/>
    /// saying how a <see cref="SyntaxTree"/> must be colored in the editor.
    /// </summary>
    [ComImport]
    [Guid( "4BBF0FC6-7D08-4761-8C81-5AEDC838C6E7" )]
    public interface IClassificationService : ICompilerService
    {
        /// <summary>
        /// Determines whether a syntax tree possibly contains compile-time code. The result is evaluated using syntax only, without
        /// getting a semantic model, and should be called before calling <see cref="GetClassifiedTextSpans"/>.
        /// </summary>
        bool ContainsCompileTimeCode( SyntaxNode syntaxRoot );

        /// <summary>
        /// Returns a set of <see cref="DesignTimeTextSpanClassification"/> saying how a <see cref="SyntaxTree"/> must be colored
        /// in the editor.
        /// </summary>
        /// <param name="semanticModel">The <see cref="SemanticModel"/> of the <see cref="SyntaxTree"/>.</param>
        /// <param name="analyzerConfigOptionsProvider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IDesignTimeClassifiedTextCollection GetClassifiedTextSpans(
            SemanticModel semanticModel,
            AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
            CancellationToken cancellationToken );
    }
}