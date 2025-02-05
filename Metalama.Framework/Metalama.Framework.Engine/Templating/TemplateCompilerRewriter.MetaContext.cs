// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Templating
{
    internal sealed partial class TemplateCompilerRewriter
    {
        /// <summary>
        /// Represents a context in which meta-statements are emitted. A context is composed of a list of meta statements
        /// and a dictionary mapping template symbols (typically local variables and local methods) to the name of the
        /// template variable that contains the name of the corresponding run-time symbol (which is determined at run time).
        ///
        /// It also defines <see cref="StatementListVariableName"/>, which is the name of the template variable that contains
        /// the current list of statement. There is typically one list per run-time block.
        /// </summary>
        private sealed class MetaContext
        {
            // Maps a local template symbol to an identifier in the generated code.
            private readonly Dictionary<ISymbol, SyntaxToken> _generatedCodeSymbolNameLocals;

            // Maps a local template symbol to an identifier in the template code.
            private readonly Dictionary<ISymbol, SyntaxToken> _templateCodeSymbolNameLocals;

            // List of unique symbols generated in the template code.
            private readonly TemplateLexicalScope _templateUniqueNames;

            private MetaContext(
                string statementListVariableName,
                Dictionary<ISymbol, SyntaxToken> generatedCodeSymbolNameLocals,
                Dictionary<ISymbol, SyntaxToken> templateCodeSymbolNameLocals,
                TemplateLexicalScope templateUniqueNames )
            {
                this.StatementListVariableName = statementListVariableName;
                this._generatedCodeSymbolNameLocals = generatedCodeSymbolNameLocals;
                this._templateCodeSymbolNameLocals = templateCodeSymbolNameLocals;
                this._templateUniqueNames = templateUniqueNames;
                this.Statements = new List<StatementSyntax>();
            }

            /// <summary>
            /// Creates a child <see cref="MetaContext"/> that corresponds to a run-time block, so it has its own
            /// <see cref="StatementListVariableName"/>.
            /// </summary>
            /// <param name="parentContext">The parent context, or <c>null</c> if we are building the root context.</param>
            public static MetaContext CreateForRunTimeBlock( MetaContext? parentContext, string statementListVariableName )
            {
                var generatedCodeSymbolNameLocals = parentContext?._generatedCodeSymbolNameLocals
                                                    ?? new Dictionary<ISymbol, SyntaxToken>( SymbolEqualityComparer.Default );

                var templateCodeSymbolNameLocals =
                    parentContext?._templateCodeSymbolNameLocals ?? new Dictionary<ISymbol, SyntaxToken>( SymbolEqualityComparer.Default );

                var templateLexicalScope = parentContext?._templateUniqueNames ?? new TemplateLexicalScope( ImmutableHashSet<string>.Empty );

                return new MetaContext( statementListVariableName, generatedCodeSymbolNameLocals, templateCodeSymbolNameLocals, templateLexicalScope );
            }

            /// <summary>
            /// Creates a child <see cref="MetaContext"/> copies everything from the parent context but has its own
            /// list of statements.
            /// </summary>
            public static MetaContext CreateHelperContext( MetaContext parentContext )
            {
                return new MetaContext(
                    parentContext.StatementListVariableName,
                    parentContext._generatedCodeSymbolNameLocals,
                    parentContext._templateCodeSymbolNameLocals,
                    parentContext._templateUniqueNames );
            }

            /// <summary>
            /// Creates a child <see cref="MetaContext"/> that corresponds to a new compile-time block (lexical scope).
            /// Symbols defined in the child scope are not defined in the parent scope.
            /// </summary>
            public static MetaContext CreateForCompileTimeBlock( MetaContext parentContext )
            {
                // Compile-time blocks are currently without effect because the dictionary maps resolved symbols, and not symbol
                // names. Two declaration of variables with the same name are still different symbols, so we don't strictly
                // need to split the dictionaries.
                // However, we're keeping this method for completeness and clarity.

                var lexicalScope = new Dictionary<ISymbol, SyntaxToken>( parentContext._generatedCodeSymbolNameLocals );

                return new MetaContext(
                    parentContext.StatementListVariableName,
                    lexicalScope,
                    parentContext._templateCodeSymbolNameLocals,
                    parentContext._templateUniqueNames );
            }

            /// <summary>
            /// Gets the name of the template variable (a List{StatementSyntax}) in which the statements for the current run-time block are being
            /// stored.
            /// </summary>
            public string StatementListVariableName { get; }

            /// <summary>
            /// Gets the list of meta-statements in the current context.
            /// </summary>
            public List<StatementSyntax> Statements { get; }

            /// <summary>
            /// Gets the name of the compiled template variable containing the name of the run-time variable corresponding
            /// to a given source template symbol (typically a local variable or a local method), if such name has been defined before.
            /// </summary>
            public bool TryGetRunTimeSymbolLocal( ISymbol symbol, out SyntaxToken templateVariableName )
                => this._generatedCodeSymbolNameLocals.TryGetValue( symbol, out templateVariableName );

            /// <summary>
            /// Maps a local template symbol (typically a local variable or a local function of the source template) to
            /// the name of the compiled template variable that contains the run-time name of the symbol. 
            /// </summary>
            public void AddRunTimeSymbolLocal( ISymbol identifierSymbol, SyntaxToken templateVariableName )
            {
                this._generatedCodeSymbolNameLocals.Add( identifierSymbol, templateVariableName );
            }

            public SyntaxToken GetTemplateVariableName( ISymbol symbol )
            {
                if ( this._templateCodeSymbolNameLocals.TryGetValue( symbol, out var value ) )
                {
                    return value;
                }
                else
                {
                    var name = this._templateUniqueNames.GetUniqueIdentifier( symbol.Name + "Name" );
                    value = SyntaxFactory.Identifier( name );
                    this._templateCodeSymbolNameLocals.Add( symbol, value );

                    return value;
                }
            }

            public SyntaxToken GetTemplateVariableName( string hint )
            {
                var name = this._templateUniqueNames.GetUniqueIdentifier( hint + "Name" );

                return SyntaxFactory.Identifier( name );
            }

            public SyntaxToken GetVariable( string hint )
            {
                var name = this._templateUniqueNames.GetUniqueIdentifier( hint );

                return SyntaxFactory.Identifier( name );
            }
        }
    }
}