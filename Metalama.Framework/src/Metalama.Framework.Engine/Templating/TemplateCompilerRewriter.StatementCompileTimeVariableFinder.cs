// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating
{
    internal partial class TemplateCompilerRewriter
    {
        /// <summary>
        /// Finds compile-time local variables assigned in a single statement (non-recursively).
        /// This includes declarations, assignments, out parameters, tuple assignments, etc.
        /// Used to determine which variables need Unsafe.SkipInit after goto labels.
        /// </summary>
        private sealed class StatementCompileTimeVariableFinder : CSharpSyntaxWalker
        {
            private readonly TemplateCompilerRewriter _parent;
            private readonly StatementSyntax _statement;

            public StatementCompileTimeVariableFinder( TemplateCompilerRewriter parent, StatementSyntax statement )
            {
                this._parent = parent;
                this._statement = statement;
            }

            public void Visit() => this.Visit( this._statement );

            public HashSet<ILocalSymbol> AssignedVariables { get; } = new( SymbolEqualityComparer.Default );

            public override void VisitLocalDeclarationStatement( LocalDeclarationStatementSyntax node )
            {
                // Check the scope annotation on the variable declaration itself
                var scope = node.GetScopeFromAnnotation();

                // Only collect compile-time-only variables that are visible after the statement
                if ( scope == TemplatingScope.CompileTimeOnly )
                {
                    foreach ( var declarator in node.Declaration.Variables )
                    {
                        this.TryAddDeclaredSymbol( declarator );
                    }
                }

                base.VisitLocalDeclarationStatement( node );
            }

            public override void VisitAssignmentExpression( AssignmentExpressionSyntax node )
            {
                switch ( node.Left.Kind() )
                {
                    // Handle regular assignments (=, +=, -=, etc.) to local variables
                    case SyntaxKind.IdentifierName when node.Left is IdentifierNameSyntax identifier:
                        this.TryAddReferencedSymbol( identifier );

                        break;

                    case SyntaxKind.TupleExpression when node.Left is TupleExpressionSyntax tupleExpression:
                        // Handle tuple assignments: (x, y) = tuple;
                        this.VisitTupleElements( tupleExpression );

                        break;
                }

                base.VisitAssignmentExpression( node );
            }

            private void VisitTupleElements( TupleExpressionSyntax tupleExpression )
            {
                foreach ( var argument in tupleExpression.Arguments )
                {
                    switch ( argument.Expression.Kind() )
                    {
                        case SyntaxKind.IdentifierName when argument.Expression is IdentifierNameSyntax identifier:
                            this.TryAddReferencedSymbol( identifier );

                            break;

                        case SyntaxKind.TupleExpression when argument.Expression is TupleExpressionSyntax nestedTuple:
                            // Handle nested tuples
                            this.VisitTupleElements( nestedTuple );

                            break;
                    }
                }
            }

            public override void VisitSingleVariableDesignation( SingleVariableDesignationSyntax node )
            {
                // This handles 'out var' declarations and pattern matching
                switch ( node.Parent?.Kind() )
                {
                    case SyntaxKind.DeclarationExpression when node.Parent is DeclarationExpressionSyntax declarationExpression:
                        {
                            var scope = declarationExpression.GetScopeFromAnnotation();

                            if ( scope == TemplatingScope.CompileTimeOnly )
                            {
                                this.TryAddDeclaredSymbol( node );
                            }

                            break;
                        }

                    case SyntaxKind.ParenthesizedVariableDesignation when node.Parent is ParenthesizedVariableDesignationSyntax parenthesizedDesignation:
                        {
                            // Handle tuple deconstruction: var (x, y) = tuple; or nested: var (a, (b, c)) = tuple;
                            // Walk up the parent chain through ParenthesizedVariableDesignation nodes to find the DeclarationExpression
                            var current = parenthesizedDesignation.Parent;

                            while ( current?.IsKind( SyntaxKind.ParenthesizedVariableDesignation ) == true
                                    && current is ParenthesizedVariableDesignationSyntax nestedParenthesized )
                            {
                                current = nestedParenthesized.Parent;
                            }

                            if ( current?.IsKind( SyntaxKind.DeclarationExpression ) == true && current is DeclarationExpressionSyntax tupleDeclaration )
                            {
                                var scope = tupleDeclaration.GetScopeFromAnnotation();

                                if ( scope == TemplatingScope.CompileTimeOnly )
                                {
                                    this.TryAddDeclaredSymbol( node );
                                }
                            }

                            break;
                        }

                    case SyntaxKind.DeclarationPattern when node.Parent is DeclarationPatternSyntax declarationPattern:
                        {
                            var scope = declarationPattern.GetScopeFromAnnotation();

                            if ( scope == TemplatingScope.CompileTimeOnly )
                            {
                                this.TryAddDeclaredSymbol( node );
                            }

                            break;
                        }
                }

                base.VisitSingleVariableDesignation( node );
            }

            private bool IsVisibleAfterStatement( ILocalSymbol localSymbol )
            {
                if ( !this._parent._syntaxTreeAnnotationMap.TryGetDeclarationAnnotation( localSymbol, out var annotation ) )
                {
                    throw new AssertionFailedException();
                }

                var localDeclarationInStatement = this._statement.GetAnnotatedNodes( annotation ).SingleOrDefault();

                // If the variable is declared in this statement at the top level, it's visible after
                if ( localDeclarationInStatement != null )
                {
                    // Make sure it's not declared inside a nested block within the statement
                    for ( var currentNode = localDeclarationInStatement.Parent;
                          currentNode != null && currentNode != this._statement;
                          currentNode = currentNode.Parent )
                    {
                        // Detect situations where the variable declaration is not visible by the parent.
                        if ( currentNode.Kind() is SyntaxKind.Block or SyntaxKind.SwitchSection or SyntaxKind.ParenthesizedExpression )
                        {
                            return false;
                        }

                        if ( currentNode is StatementSyntax )
                        {
                            return false;
                        }
                    }

                    return true;
                }

                // If the variable is declared in an outer scope (before this statement), it's visible after
                for ( var statementParent = this._statement.Parent; statementParent != null; statementParent = statementParent.Parent )
                {
                    if ( statementParent.GetAnnotatedNodes( annotation ).Any() )
                    {
                        return true;
                    }
                }

                return false;
            }

            private void TryAddDeclaredSymbol( SyntaxNode node )
            {
                var symbol = this._parent._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

                this.TryAddReferencedSymbol( symbol );
            }

            private void TryAddReferencedSymbol( SyntaxNode node )
            {
                var scope = node.GetScopeFromAnnotation();

                if ( scope == TemplatingScope.CompileTimeOnly )
                {
                    var symbol = this._parent._syntaxTreeAnnotationMap.GetSymbol( node );

                    this.TryAddReferencedSymbol( symbol );
                }
            }

            private void TryAddReferencedSymbol( ISymbol? symbol )
            {
                if ( symbol?.Kind == SymbolKind.Local && symbol is ILocalSymbol localSymbol && this.IsVisibleAfterStatement( localSymbol ) )
                {
                    this.AssignedVariables.Add( localSymbol );
                }
            }
        }
    }
}