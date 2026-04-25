// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;

namespace Metalama.Extensions.Metrics
{
    /// <summary>
    /// Provides the implementation for the <see cref="LinesOfCode"/> metric.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider computes lines of code directly from syntax nodes without aggregation,
    /// which correctly handles cases where multiple declarations share the same line (e.g., fields).
    /// </para>
    /// <para>
    /// The implementation uses an O(1) space algorithm that tracks the maximum line number seen
    /// while iterating through tokens in document order. Multi-line tokens (such as verbatim strings
    /// or raw string literals) are correctly counted as multiple lines.
    /// </para>
    /// </remarks>
    /// <seealso cref="LinesOfCode"/>
    /// <seealso href="@custom-metrics"/>
    [MetalamaPlugIn]
    public sealed class LinesOfCodeMetricProvider : MetricProvider<LinesOfCode>
    {
        protected override LinesOfCode ComputeMetricForType( INamedType namedType ) => ComputeMetric( namedType );

        protected override LinesOfCode ComputeMetricForMember( IMember member ) => ComputeMetric( member );

        protected override void Aggregate( ref LinesOfCode aggregate, in LinesOfCode newValue ) => aggregate.Add( newValue );

        private static LinesOfCode ComputeMetric( IDeclaration declaration )
        {
            var symbol = declaration.GetSymbol();

            if ( symbol == null )
            {
                // Not source code.
                return default;
            }

            var aggregate = default(LinesOfCode);

            foreach ( var syntaxRef in GetAllSyntaxReferences( symbol ) )
            {
                var newValue = ComputeForSyntaxNode( syntaxRef.GetSyntax() );
                aggregate.Add( newValue );
            }

            return aggregate;
        }

        /// <summary>
        /// Gets all syntax references for a symbol, including both parts of partial methods and properties.
        /// </summary>
        /// <remarks>
        /// For partial types (classes, structs, interfaces), <see cref="ISymbol.DeclaringSyntaxReferences"/>
        /// already includes all partial declarations. For partial methods and properties, we need to
        /// explicitly collect references from both the definition and implementation parts.
        /// </remarks>

        // TODO: Add support for partial properties (C# 13), events and constructors (C# 14).
        private static IEnumerable<SyntaxReference> GetAllSyntaxReferences( ISymbol symbol )
        {
            switch ( symbol )
            {
                case IMethodSymbol methodSymbol:
                    // For partial methods, aggregate both the definition and implementation parts.
                    var definitionPart = methodSymbol.PartialDefinitionPart ?? methodSymbol;
                    var implementationPart = methodSymbol.PartialImplementationPart;

                    foreach ( var syntaxRef in definitionPart.DeclaringSyntaxReferences )
                    {
                        yield return syntaxRef;
                    }

                    if ( implementationPart != null && !ReferenceEquals( implementationPart, definitionPart ) )
                    {
                        foreach ( var syntaxRef in implementationPart.DeclaringSyntaxReferences )
                        {
                            yield return syntaxRef;
                        }
                    }

                    break;

                default:
                    // For partial types and other symbols, DeclaringSyntaxReferences already includes all parts.
                    foreach ( var syntaxRef in symbol.DeclaringSyntaxReferences )
                    {
                        yield return syntaxRef;
                    }

                    break;
            }
        }

        private static LinesOfCode ComputeForSyntaxNode( SyntaxNode node )
        {
            var maxLogicalLine = -1;
            var logicalCount = 0;

            foreach ( var token in node.DescendantTokens() )
            {
                // Logical: skip brace tokens.
                if ( token.IsKind( SyntaxKind.OpenBraceToken ) || token.IsKind( SyntaxKind.CloseBraceToken ) )
                {
                    continue;
                }

                var span = token.GetLocation().GetLineSpan();
                var startLine = span.StartLinePosition.Line;
                var endLine = span.EndLinePosition.Line;

                if ( endLine > maxLogicalLine )
                {
                    logicalCount += endLine - Math.Max( maxLogicalLine, startLine - 1 );
                    maxLogicalLine = endLine;
                }
            }

            // Total and NonBlank: calculated from source text to include inactive preprocessor branches.
            var nodeSpan = node.GetLocation().GetLineSpan();
            var startLine2 = nodeSpan.StartLinePosition.Line;
            var endLine2 = nodeSpan.EndLinePosition.Line;
            var totalCount = endLine2 - startLine2 + 1;

            // NonBlank: count lines with non-whitespace content in the source text.
            var nonBlankCount = 0;
            var sourceText = node.SyntaxTree.GetText();

            for ( var lineIndex = startLine2; lineIndex <= endLine2; lineIndex++ )
            {
                var lineSpan = sourceText.Lines[lineIndex].Span;

                for ( var i = lineSpan.Start; i < lineSpan.End; i++ )
                {
                    if ( !char.IsWhiteSpace( sourceText[i] ) )
                    {
                        nonBlankCount++;

                        break;
                    }
                }
            }

            return new LinesOfCode { Logical = logicalCount, NonBlank = nonBlankCount, Total = totalCount };
        }
    }
}