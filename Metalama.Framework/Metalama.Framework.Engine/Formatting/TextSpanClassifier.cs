// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// Produces a <see cref="ClassifiedTextSpanCollection"/> with compile-time code given
    /// a syntax tree annotated with <see cref="TemplateAnnotator"/>.
    /// </summary>
    internal sealed partial class TextSpanClassifier : ClassifierBase
    {
        private readonly CancellationToken _cancellationToken;
#if !DEBUG
#pragma warning restore IDE0052 // Remove unread private members
#endif
        private readonly string _sourceString;
        private readonly MarkAllChildrenWalker _markAllChildrenWalker;
        private bool _isInTemplate;
        private bool _isInCompileTimeType;
        private bool _isRecursiveCall;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextSpanClassifier"/> class and specifies a backward-compatibility flag.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="cancellationToken"></param>
        public TextSpanClassifier( SourceText sourceText, CancellationToken cancellationToken )
            : base( new ClassifiedTextSpanCollection( sourceText ), sourceText )
        {
            this._cancellationToken = cancellationToken;
            this._sourceString = sourceText.ToString();
            this._markAllChildrenWalker = new MarkAllChildrenWalker( this );
        }

        private static bool ShouldMarkTrivia( TextSpanClassification classification )
            => classification switch
            {
                TextSpanClassification.CompileTime => true,
                TextSpanClassification.RunTime => true,
                _ => false
            };

        protected override void VisitCore( SyntaxNode? node )
        {
            if ( node == null )
            {
                // Coverage: ignore.
                return;
            }

            this._cancellationToken.ThrowIfCancellationRequested();

            var isRecursiveCall = this._isRecursiveCall;
            this._isRecursiveCall = true;

            try
            {
                base.VisitCore( node );
            }
            finally
            {
                this._isRecursiveCall = isRecursiveCall;
            }
        }

        private bool VisitTypeDeclaration<T>( T node, Action<T> visitBase )
            where T : TypeDeclarationSyntax
        {
            var scope = node.GetScopeFromAnnotation();

            if ( scope is TemplatingScope.CompileTimeOnly or TemplatingScope.RunTimeOrCompileTime )
            {
                var oldIsInCompileTimeType = this._isInCompileTimeType;

                this._isInCompileTimeType = true;

                try
                {
                    this.Mark( node.Modifiers, TextSpanClassification.CompileTime );
                    this.Mark( node.Keyword, TextSpanClassification.CompileTime );
                    this.Mark( node.OpenBraceToken, TextSpanClassification.CompileTime );
                    this.Mark( node.CloseBraceToken, TextSpanClassification.CompileTime );
                    this.Mark( node.ConstraintClauses, TextSpanClassification.CompileTime );
                    this.Mark( node.Identifier, TextSpanClassification.CompileTime );
                    this.Mark( node.BaseList, TextSpanClassification.CompileTime );
                    this.Mark( node.AttributeLists, TextSpanClassification.CompileTime );
                    this.Mark( node.TypeParameterList, TextSpanClassification.CompileTime );

                    visitBase( node );

                    return true;
                }
                finally
                {
                    this._isInCompileTimeType = oldIsInCompileTimeType;
                }
            }

            return false;
        }

        public override void VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitClassDeclaration( n ) );

        public override void VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node, n => base.VisitStructDeclaration( n ) );

        public override void VisitRecordDeclaration( RecordDeclarationSyntax node )
        {
            if ( this.VisitTypeDeclaration( node, n => base.VisitRecordDeclaration( n ) ) )
            {
                this.Mark( node.ParameterList, TextSpanClassification.CompileTime );
            }
        }

        private void VisitSimpleTypeDeclaration<T>( T node, Action<T> visitBase )
            where T : SyntaxNode
        {
            var scope = node.GetScopeFromAnnotation();

            if ( scope is TemplatingScope.CompileTimeOnly or TemplatingScope.RunTimeOrCompileTime )
            {
                this.Mark( node, TextSpanClassification.CompileTime );

                visitBase( node );
            }
        }

        public override void VisitDelegateDeclaration( DelegateDeclarationSyntax node )
            => this.VisitSimpleTypeDeclaration( node, base.VisitDelegateDeclaration );

        public override void VisitEnumDeclaration( EnumDeclarationSyntax node ) => this.VisitSimpleTypeDeclaration( node, base.VisitEnumDeclaration );

        public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
        {
            if ( node.IsTemplateFromAnnotation() )
            {
                this._isInTemplate = true;

                // The code is run-time by default in a template method.
                this.Mark( node.Body, TextSpanClassification.RunTime );
                this.Mark( node.ExpressionBody, TextSpanClassification.RunTime );

                base.VisitMethodDeclaration( node );

                this._isInTemplate = false;
            }
            else
            {
                this.VisitMember( node, base.VisitMethodDeclaration );
            }
        }

        private void VisitMember<T>( T node, Action<T> callBase )
            where T : SyntaxNode
        {
            if ( this._isInCompileTimeType )
            {
                this.Mark( node, TextSpanClassification.CompileTime );

                // We still visit the member to mark indentation.
                callBase( node );
            }
        }

        public override void VisitFieldDeclaration( FieldDeclarationSyntax node ) => this.VisitMember( node, base.VisitFieldDeclaration );

        public override void VisitEventDeclaration( EventDeclarationSyntax node ) => this.VisitMember( node, base.VisitEventDeclaration );

        public override void VisitPropertyDeclaration( PropertyDeclarationSyntax node )
        {
            if ( node.IsTemplateFromAnnotation() ||
                 (node.AccessorList != null && node.AccessorList.Accessors.Any( a => a.IsTemplateFromAnnotation() )) )
            {
                this._isInTemplate = true;

                // The code is run-time by default in a template method.
                this.Mark( node.ExpressionBody, TextSpanClassification.RunTime );

                base.VisitPropertyDeclaration( node );

                this._isInTemplate = false;
            }
            else
            {
                this.VisitMember( node, base.VisitPropertyDeclaration );
            }
        }

        public override void VisitAccessorDeclaration( AccessorDeclarationSyntax node )
        {
            if ( this._isInTemplate )
            {
                this.Mark( node.Body, TextSpanClassification.RunTime );

                if ( node.ExpressionBody != null )
                {
                    this.Mark( node.ExpressionBody.Expression, TextSpanClassification.RunTime );
                }
            }

            base.VisitAccessorDeclaration( node );
        }

        public override void VisitEventFieldDeclaration( EventFieldDeclarationSyntax node ) => this.VisitMember( node, base.VisitEventFieldDeclaration );

        public override void VisitToken( SyntaxToken token )
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            if ( this._isInTemplate )
            {
                var colorFromAnnotation = token.GetColorFromAnnotation();

                if ( colorFromAnnotation != TextSpanClassification.Default )
                {
                    this.Mark( token, colorFromAnnotation );
                }
            }

            base.VisitToken( token );
        }

        public override void DefaultVisit( SyntaxNode node )
        {
            if ( this._isInTemplate )
            {
                if ( TemplateCompilerRewriter.IsCompileTimeCode( node ) )
                {
                    // This can be overwritten later in a child node.
                    this.Mark( node, TextSpanClassification.CompileTime );
                }

                // Mark the node.
                var colorFromAnnotation = node.GetColorFromAnnotation();

                if ( colorFromAnnotation != TextSpanClassification.Default )
                {
                    this.Mark( node, colorFromAnnotation );
                }
            }

            base.DefaultVisit( node );
        }

        private void Mark( SyntaxNode? node, TextSpanClassification classification )
        {
            if ( node != null )
            {
                this._markAllChildrenWalker.MarkAll( node, classification );
            }
        }

        private void Mark( SyntaxTokenList list, TextSpanClassification classification )
        {
            foreach ( var item in list )
            {
                this.Mark( item, classification );
            }
        }

        private void Mark<T>( SyntaxList<T> list, TextSpanClassification classification )
            where T : SyntaxNode
        {
            foreach ( var item in list )
            {
                this.Mark( item, classification );
            }
        }

        private void Mark( SyntaxToken token, TextSpanClassification classification )
        {
            if ( token.IsKind( SyntaxKind.XmlTextLiteralNewLineToken ) )
            {
                // Don't highlight end-of-lines in XML comments.
                return;
            }

            this.Mark( token.Span, classification );

            if ( ShouldMarkTrivia( classification ) )
            {
                this.Mark( token.LeadingTrivia, classification );
                this.Mark( token.TrailingTrivia, classification );
            }
        }

        private void Mark( SyntaxTriviaList triviaList, TextSpanClassification classification )
        {
            foreach ( var trivia in triviaList )
            {
                if ( trivia.Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or SyntaxKind.EndOfLineTrivia )
                {
                    // Don't highlight comments and end-of-lines.
                    continue;
                }

                switch ( trivia.GetStructure() )
                {
                    case DocumentationCommentTriviaSyntax documentationComment:

                        // Mark each node of a documentation comment trivia separately.
                        foreach ( var node in documentationComment.ChildNodes() )
                        {
                            this.Mark( node, classification );
                        }

                        continue;

                    case not null:

                        // Don't highlight #directives and skipped tokens.
                        continue;
                }

                var triviaStart = trivia.FullSpan.Start;
                var triviaEnd = Math.Min( trivia.FullSpan.End, this._sourceString.Length - 1 );

                if ( triviaStart > triviaEnd )
                {
                    // This should not happen.
                    continue;
                }

                if ( triviaStart != triviaEnd )
                {
                    this.ClassifiedTextSpans.Add( TextSpan.FromBounds( triviaStart, triviaEnd ), classification );
                }
            }
        }

        /*
        public override void VisitLiteralExpression( LiteralExpressionSyntax node )
        {
            // We don't mark literals that are not a part of larger compile-time expressions because it does not bring anything useful.
        }
        */

        public override void VisitIfStatement( IfStatementSyntax node )
        {
            this.VisitToken( node.IfKeyword );

            if ( this._isInTemplate && TemplateCompilerRewriter.IsCompileTimeCode( node ) )
            {
                this.Mark( TextSpan.FromBounds( node.IfKeyword.SpanStart, node.CloseParenToken.Span.End ), TextSpanClassification.CompileTime );
                this.VisitToken( node.OpenParenToken );
                this.Visit( node.Condition );
                this.VisitToken( node.CloseParenToken );
                this.Visit( node.Statement );

                if ( node.Else != null )
                {
                    this.Mark( node.Else.ElseKeyword, TextSpanClassification.CompileTime );
                    this.VisitToken( node.Else.ElseKeyword );
                    this.Visit( node.Else.Statement );
                }
            }
            else
            {
                base.VisitIfStatement( node );
            }
        }

        public override void VisitForEachStatement( ForEachStatementSyntax node )
        {
            if ( this._isInTemplate && TemplateCompilerRewriter.IsCompileTimeCode( node ) )
            {
                this.VisitToken( node.ForEachKeyword );

                this.Mark( TextSpan.FromBounds( node.ForEachKeyword.SpanStart, node.CloseParenToken.Span.End ), TextSpanClassification.CompileTime );
                this.Mark( node.Identifier, TextSpanClassification.CompileTimeVariable );
                this.VisitToken( node.OpenParenToken );
                this.Visit( node.Expression );
                this.VisitToken( node.CloseParenToken );
                this.Visit( node.Statement );
            }
            else
            {
                base.VisitForEachStatement( node );
            }
        }

        public override void VisitBlock( BlockSyntax node )
        {
            if ( this._isInTemplate && TemplateCompilerRewriter.IsCompileTimeCode( node ) )
            {
                this.Mark( node.OpenBraceToken, TextSpanClassification.CompileTime );
                this.Mark( node.CloseBraceToken, TextSpanClassification.CompileTime );

                this.VisitToken( node.OpenBraceToken );

                foreach ( var statement in node.Statements )
                {
                    this.Visit( statement );
                }

                this.VisitToken( node.CloseBraceToken );
            }
            else
            {
                base.VisitBlock( node );
            }
        }
    }
}