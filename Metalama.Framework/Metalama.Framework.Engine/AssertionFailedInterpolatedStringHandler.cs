// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Metalama.Framework.Engine;

/// <summary>
/// Improves errors by enhancing specific values when creating AssertionFailedException.
/// </summary>
[InterpolatedStringHandler]
internal readonly ref struct AssertionFailedInterpolatedStringHandler
{
    private readonly StringBuilder _builder;

    public AssertionFailedInterpolatedStringHandler( int literalLength, [UsedImplicitly] int formattedCount )
    {
        this._builder = new StringBuilder( literalLength );
    }

    public void AppendLiteral( string s ) => this._builder.Append( s );

    public void AppendFormatted<T>( T? value )
    {
        try
        {
            switch ( value )
            {
                case ISymbol symbol:
                    this._builder.Append( FormatSymbol( symbol ) );

                    break;

                case SyntaxNode syntaxNode:
                    this._builder.Append( FormatSyntaxNode( syntaxNode ) );

                    break;

                default:
                    this._builder.Append( value?.ToString() ?? "(null)" );

                    break;
            }
        }
        catch ( Exception e )
        {
            this._builder.AppendInvariant( $"{{'{e}'}}" );
        }
    }

    private static string FormatSymbol( ISymbol? symbol )
    {
        switch ( symbol )
        {
            case null:
                return "(null)";

            case IParameterSymbol parameterSymbol:
                return $"{SymbolKind.Parameter}:{parameterSymbol.ContainingSymbol}:{parameterSymbol}{FormatLocations( parameterSymbol.Locations )}";

            case IErrorTypeSymbol errorSymbol:
                return
                    $"{SymbolKind.ErrorType}:<{errorSymbol.CandidateReason}>({string.Join( ",", errorSymbol.CandidateSymbols.Select( FormatSymbol ) )}){FormatLocations( errorSymbol.Locations )}";

            default:
                var symbolString = symbol.ToString();

                if ( string.IsNullOrEmpty( symbolString ) )
                {
                    return $"{symbol.Kind}:({FormatSymbol( symbol.ContainingSymbol )}).<empty>{FormatLocations( symbol.Locations )}";
                }
                else
                {
                    return $"{symbol.Kind}:{symbolString}{FormatLocations( symbol.Locations )}";
                }
        }
    }

    private static string FormatSyntaxNode( SyntaxNode node )
    {
        /*
         * Attempts to render a shallow structure of the syntax node. This works by "masking" some tokens in descendant tokens of this node,
         * while ignoring all the trivia. Masking of a node is done if it exceeds masking depth (current limit is 1) and removes tokens from
         * syntax kind in <> angle brackets, e.g.  <SimpleMemberAccessExpression>(ref <SimpleMemberAccessExpression>).
         * Lists (type members, block statements, switch sections, etc.) are simplified by using ellipses, i.e. <...>.
         */

        var walker = new MaskingDepthWalker( 1 );
        walker.Visit( node );

        return RenderSyntaxNode( node, walker.MaskedNodes, walker.EllipsisSpans );
    }

    private static string FormatLocations( ImmutableArray<Location> locations )
    {
        if ( locations.IsEmpty )
        {
            return "";
        }
        else
        {
            var firstLocation = locations.First();

            return FormatLocation( firstLocation, ":[", "]" );
        }
    }

    private static string FormatLocation( Location location, string prefix, string suffix )
    {
        if ( location.SourceTree != null )
        {
            var lineSpan = location.SourceTree.GetLineSpan( location.SourceSpan );
            var filePath = Path.GetFileName( location.SourceTree.FilePath );

            return $"{prefix}({lineSpan.StartLinePosition})-({lineSpan.EndLinePosition}){(filePath.Length > 0 ? $":{filePath}" : filePath)}{suffix}";
        }
        else
        {
            return $"{prefix}{location}{suffix}";
        }
    }

    internal string GetFormattedText() => this._builder.ToString();

    private static string RenderSyntaxNode( SyntaxNode rootNode, IEnumerable<SyntaxNode> maskedNodes, IEnumerable<TextSpan> ellipsisSpan )
    {
        var builder = new StringBuilder();
        var sortedMaskedNodes = maskedNodes.OrderBy( n => n.SpanStart ).ToArray();
        var sortedEllipsisSpans = ellipsisSpan.OrderBy( s => s.Start ).ToArray();

        // Presume that masked spans do not intersect.

        var currentMasked = 0;
        var currentEllipsis = 0;
        var appendLeadingSpace = false;

        var rootLocation = rootNode.GetLocation();
        var locationText = FormatLocation( rootLocation, ":", "" );

        builder.AppendInvariant( $"[{rootNode.Kind()}{locationText}] " );

        foreach ( var token in rootNode.DescendantTokens() )
        {
            // Repeat until we have span that does not start in the current masked node.
            while ( currentMasked < sortedMaskedNodes.Length && token.Span.Start > sortedMaskedNodes[currentMasked].Span.End - 1 )
            {
                builder.AppendInvariant( $"<{sortedMaskedNodes[currentMasked].Kind()}>" );

                if ( sortedMaskedNodes[currentMasked].GetTrailingTrivia().Any( t => t.Kind() is SyntaxKind.WhitespaceTrivia ) )
                {
                    // Write space if there was any trailing whitespace trivia.
                    builder.Append( ' ' );
                    appendLeadingSpace = false;
                }
                else
                {
                    appendLeadingSpace = true;
                }

                currentMasked++;
            }

            // Repeat until we have span that does not start in the current masked node.
            while ( currentEllipsis < sortedEllipsisSpans.Length && token.Span.Start > sortedEllipsisSpans[currentEllipsis].End - 1 )
            {
                builder.AppendInvariant( $"<...>" );

                appendLeadingSpace = false;
                currentEllipsis++;
            }

            if ( (currentMasked < sortedMaskedNodes.Length && token.Span.Intersection( sortedMaskedNodes[currentMasked].Span )?.Length > 0)
                 || (currentEllipsis < sortedEllipsisSpans.Length && token.Span.Intersection( sortedEllipsisSpans[currentEllipsis] )?.Length > 0) )
            {
                // Token is masked or in ellipsis.
                continue;
            }

            if ( appendLeadingSpace && token.LeadingTrivia.Any( t => t.Kind() is SyntaxKind.WhitespaceTrivia ) )
            {
                // Write space if there was any leading whitespace trivia and previous token did not have whitespace trailing trivia.
                builder.Append( ' ' );
            }

            // Write token.
            switch ( token.Kind() )
            {
                case SyntaxKind.IdentifierToken:
                    builder.Append( token.ValueText != "var" ? "<identifier>" : "var" );

                    break;

                case SyntaxKind.NumericLiteralToken:
                    builder.Append( "<numeric_literal>" );

                    break;

                case SyntaxKind.CharacterLiteralToken:
                    builder.Append( "<char_literal>" );

                    break;

                case SyntaxKind.StringLiteralToken:
                    builder.Append( "<string_literal>" );

                    break;

#if ROSLYN_4_8_0_OR_GREATER
                case SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken:
                    builder.Append( "<raw_string_literal>" );

                    break;

                case SyntaxKind.Utf8StringLiteralToken:
                    builder.Append( "<utf8_string_literal>" );

                    break;

                case SyntaxKind.Utf8SingleLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                    builder.Append( "<utf8_raw_string_literal>" );

                    break;
#endif

                case SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlTextLiteralNewLineToken or SyntaxKind.XmlEntityLiteralToken:
                    builder.Append( "<xml_literal>" );

                    break;

                default:
                    builder.Append( token );

                    break;
            }

            if ( token.TrailingTrivia.Any( t => t.Kind() is SyntaxKind.WhitespaceTrivia ) )
            {
                // Write space if there was any trailing whitespace trivia.
                builder.Append( ' ' );
                appendLeadingSpace = false;
            }
            else
            {
                appendLeadingSpace = true;
            }
        }

        while ( currentMasked < sortedMaskedNodes.Length )
        {
            builder.AppendInvariant( $"<{sortedMaskedNodes[currentMasked].Kind()}>" );

            if ( sortedMaskedNodes[currentMasked].GetTrailingTrivia().Any( t => t.Kind() is SyntaxKind.WhitespaceTrivia ) )
            {
                // Write space if there was any trailing whitespace trivia.
                builder.Append( ' ' );
            }

            currentMasked++;
        }

        // Repeat until we have span that does not start in the current masked node.
        while ( currentEllipsis < sortedEllipsisSpans.Length )
        {
            builder.AppendInvariant( $"<...>" );
            currentEllipsis++;
        }

        return builder.ToString();
    }

    private sealed class MaskingDepthWalker : SafeSyntaxWalker
    {
        private readonly int _maskingDepthLimit;
        private int _currentMaskingDepth;

        public List<SyntaxNode> MaskedNodes { get; }

        public List<TextSpan> EllipsisSpans { get; }

        public MaskingDepthWalker( int maskingDepthLimit )
        {
            this.MaskedNodes = new List<SyntaxNode>();
            this.EllipsisSpans = new List<TextSpan>();
            this._maskingDepthLimit = maskingDepthLimit;
        }

        protected override void VisitCore( SyntaxNode? node )
        {
            if ( node == null )
            {
                return;
            }

            if ( this._currentMaskingDepth > this._maskingDepthLimit )
            {
                this.MaskedNodes.Add( node );

                return;
            }

            var increaseMaskingDepth =
                node switch
                {
                    ExpressionStatementSyntax => false,
                    GenericNameSyntax => false,
                    QualifiedNameSyntax => false,
                    AliasQualifiedNameSyntax => false,
                    AccessorListSyntax => false,
                    AccessorDeclarationSyntax => false,
                    BlockSyntax { Parent: ElseClauseSyntax } => false,
                    IdentifierNameSyntax { Identifier.ValueText: "var" } => false,
                    _ => true
                };

            if ( increaseMaskingDepth )
            {
                try
                {
                    this._currentMaskingDepth++;

                    base.VisitCore( node );
                }
                finally
                {
                    this._currentMaskingDepth--;
                }
            }
            else
            {
                base.VisitCore( node );
            }
        }

        public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override void VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override void VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override void VisitEnumDeclaration( EnumDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override void VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        private void VisitTypeDeclaration( BaseTypeDeclarationSyntax node )
        {
            // Type needs to only mask it's members.

            if ( this._currentMaskingDepth - 1 > this._maskingDepthLimit )
            {
                this.MaskedNodes.Add( node );

                return;
            }

            try
            {
                this._currentMaskingDepth++;

                if ( this._currentMaskingDepth > this._maskingDepthLimit )
                {
                    switch ( node )
                    {
                        case TypeDeclarationSyntax typeDeclaration:
                            this.EllipsisSpans.Add( typeDeclaration.Members.Span );

                            break;

                        case EnumDeclarationSyntax enumDeclaration:
                            this.EllipsisSpans.Add( enumDeclaration.Members.Span );

                            break;
                    }
                }
            }
            finally
            {
                this._currentMaskingDepth--;
            }
        }

        public override void VisitBlock( BlockSyntax node )
        {
            if ( this._currentMaskingDepth >= this._maskingDepthLimit )
            {
                if ( node.Statements.Span.Length > 0 )
                {
                    this.EllipsisSpans.Add( node.Statements.Span );
                }

                return;
            }

            foreach ( var statement in node.Statements )
            {
                base.VisitCore( statement );
            }
        }

#if ROSLYN_4_8_0_OR_GREATER
        public override void VisitCollectionExpression( CollectionExpressionSyntax node )
        {
            if ( this._currentMaskingDepth >= this._maskingDepthLimit )
            {
                if ( node.Elements.Span.Length > 0 )
                {
                    this.EllipsisSpans.Add( node.Elements.Span );
                }

                return;
            }

            foreach ( var element in node.Elements )
            {
                base.VisitCore( element );
            }
        }
#endif

        public override void VisitInitializerExpression( InitializerExpressionSyntax node )
        {
            if ( this._currentMaskingDepth >= this._maskingDepthLimit )
            {
                if ( node.Expressions.Span.Length > 0 )
                {
                    this.EllipsisSpans.Add( node.Expressions.Span );
                }

                return;
            }

            foreach ( var expression in node.Expressions )
            {
                base.VisitCore( expression );
            }
        }

        public override void VisitSwitchExpression( SwitchExpressionSyntax node )
        {
            if ( this._currentMaskingDepth >= this._maskingDepthLimit )
            {
                if ( node.Arms.Span.Length > 0 )
                {
                    this.EllipsisSpans.Add( node.Arms.Span );
                }

                return;
            }

            foreach ( var arm in node.Arms )
            {
                base.VisitCore( arm );
            }
        }

        public override void VisitSwitchStatement( SwitchStatementSyntax node )
        {
            if ( this._currentMaskingDepth >= this._maskingDepthLimit )
            {
                if ( node.Sections.Span.Length > 0 )
                {
                    this.EllipsisSpans.Add( node.Sections.Span );
                }

                return;
            }

            foreach ( var section in node.Sections )
            {
                base.VisitCore( section );
            }
        }
    }
}