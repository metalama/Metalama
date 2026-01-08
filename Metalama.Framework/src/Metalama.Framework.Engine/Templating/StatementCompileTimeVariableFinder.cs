// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Templating
{
    /// <summary>
    /// Finds compile-time local variables assigned in a single statement (non-recursively).
    /// This includes declarations, assignments, out parameters, tuple assignments, etc.
    /// Used to determine which variables need Unsafe.SkipInit after goto labels.
    /// </summary>
    internal sealed class StatementCompileTimeVariableFinder : CSharpSyntaxWalker
    {
        private readonly SyntaxTreeAnnotationMap _syntaxTreeAnnotationMap;
        private readonly StatementSyntax _statement;
        
        public StatementCompileTimeVariableFinder( SyntaxTreeAnnotationMap syntaxTreeAnnotationMap, StatementSyntax statement )
        {
            this._syntaxTreeAnnotationMap = syntaxTreeAnnotationMap;
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
            // Handle regular assignments (=, +=, -=, etc.) to local variables
            if ( node.Left is IdentifierNameSyntax identifier )
            {
                this.TryAddReferencedSymbol( identifier );
            }
            else if ( node.Left is TupleExpressionSyntax tupleExpression )
            {
                // Handle tuple assignments: (x, y) = tuple;
                this.VisitTupleElements( tupleExpression );
            }

            base.VisitAssignmentExpression( node );
        }

        private void VisitTupleElements( TupleExpressionSyntax tupleExpression )
        {
            foreach ( var argument in tupleExpression.Arguments )
            {
                if ( argument.Expression is IdentifierNameSyntax identifier )
                {
                    this.TryAddReferencedSymbol( identifier );
                }
                else if ( argument.Expression is TupleExpressionSyntax nestedTuple )
                {
                    // Handle nested tuples
                    this.VisitTupleElements( nestedTuple );
                }
            }
        }

        public override void VisitSingleVariableDesignation( SingleVariableDesignationSyntax node )
        {
            // This handles 'out var' declarations and pattern matching
            if ( node.Parent is DeclarationExpressionSyntax declarationExpression )
            {
                var scope = declarationExpression.GetScopeFromAnnotation();

                if ( scope == TemplatingScope.CompileTimeOnly )
                {
                    this.TryAddDeclaredSymbol( node );
                }
            }
            else if ( node.Parent is DeclarationPatternSyntax declarationPattern )
            {
                var scope = declarationPattern.GetScopeFromAnnotation();

                if ( scope == TemplatingScope.CompileTimeOnly )
                {
                    this.TryAddDeclaredSymbol( node );
                }
            }

            base.VisitSingleVariableDesignation( node );
        }

        private bool IsVisibleAfterStatement( ILocalSymbol localSymbol )
        {
            var declaringSyntax = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

            if ( declaringSyntax == null )
            {
                return false;
            }

            // `declaringSyntax` is defined in the non-annotated syntax tree. We must find the same node in the annotated syntax tree.
            // Find the ancestor declaration (method, property, etc.) as the root for the search.
            var sourceRoot = declaringSyntax.FindMemberDeclaration();
            var targetRoot = this._statement.FindMemberDeclaration();

            if ( !Utilities.Roslyn.SyntaxExtensions.TryFindNodeByPosition( declaringSyntax, sourceRoot, targetRoot, out var annotatedDeclaringSyntax )
                 || annotatedDeclaringSyntax == null )
            {
                return false;
            }

            // If the variable is declared in this statement at the top level, it's visible after
            if ( this._statement.Contains( annotatedDeclaringSyntax ) )
            {
                // Make sure it's not declared inside a nested block within the statement
                for ( var currentNode = annotatedDeclaringSyntax.Parent; currentNode != null && currentNode != this._statement; currentNode = currentNode.Parent )
                {
                    // If we encounter a block before reaching the statement, the variable is nested
                    if ( currentNode is BlockSyntax )
                    {
                        return false;
                    }
                }

                return true;
            }

            // If the variable is declared in an outer scope (before this statement), it's visible after
            for ( var statementParent = this._statement.Parent; statementParent != null; statementParent = statementParent.Parent )
            {
                if ( statementParent.Contains( annotatedDeclaringSyntax ) )
                {
                    return true;
                }
            }

            return false;
        }

        private void TryAddDeclaredSymbol( SyntaxNode node )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

            this.TryAddReferencedSymbol( symbol );
        }

        private void TryAddReferencedSymbol( SyntaxNode node )
        {
            var scope = node.GetScopeFromAnnotation();

            if ( scope == TemplatingScope.CompileTimeOnly )
            {
                var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

                this.TryAddReferencedSymbol( symbol );
            }
        }

        private void TryAddReferencedSymbol( ISymbol? symbol )
        {
            if ( symbol is ILocalSymbol localSymbol && this.IsVisibleAfterStatement( localSymbol ) )
            {
                this.AssignedVariables.Add( localSymbol );
            }
        }
    }
}
