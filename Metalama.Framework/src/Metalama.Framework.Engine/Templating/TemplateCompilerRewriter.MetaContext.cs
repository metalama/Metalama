// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
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
                TemplateLexicalScope templateUniqueNames,
                MetaContext? parent = null,
                bool isRunTimeBlock = false,
                bool isCompileTimeConditionalBlock = false )
            {
                this.StatementListVariableName = statementListVariableName;
                this._generatedCodeSymbolNameLocals = generatedCodeSymbolNameLocals;
                this._templateCodeSymbolNameLocals = templateCodeSymbolNameLocals;
                this._templateUniqueNames = templateUniqueNames;
                this.Statements = new List<StatementSyntax>();
                this.Parent = parent;
                this.IsRunTimeBlock = isRunTimeBlock;
                this.IsCompileTimeConditionalBlock = isCompileTimeConditionalBlock;
            }

            /// <summary>
            /// Gets the parent context, or <c>null</c> if this is the root context.
            /// </summary>
            public MetaContext? Parent { get; }

            /// <summary>
            /// Gets a value indicating whether this context corresponds to a run-time block.
            /// </summary>
            public bool IsRunTimeBlock { get; }

            /// <summary>
            /// Gets a value indicating whether this context corresponds to a compile-time conditional block
            /// (e.g., the body of a compile-time if/while/for statement).
            /// </summary>
            public bool IsCompileTimeConditionalBlock { get; }

            /// <summary>
            /// Determines whether this context or any of its ancestors is a run-time block.
            /// A return statement inside a run-time block should not terminate compile-time flow.
            /// </summary>
            public bool IsInsideRunTimeBlock()
            {
                for ( var context = this; context != null; context = context.Parent )
                {
                    if ( context.IsRunTimeBlock )
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Determines whether this context or any of its ancestors is a compile-time conditional block.
            /// Only returns inside compile-time conditionals should terminate compile-time flow.
            /// </summary>
            public bool IsInsideCompileTimeConditionalBlock()
            {
                for ( var context = this; context != null; context = context.Parent )
                {
                    if ( context.IsCompileTimeConditionalBlock )
                    {
                        return true;
                    }
                }

                return false;
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

                // Only mark as a run-time block if there's a parent context.
                // The root context (parentContext == null) is the template body itself and should not
                // be considered a "run-time block" for the purpose of compile-time flow termination.
                // A "run-time block" in this context means a block inside a run-time conditional
                // (like if(runtime_condition)), where returns should not terminate compile-time flow.

                return new MetaContext(
                    statementListVariableName,
                    generatedCodeSymbolNameLocals,
                    templateCodeSymbolNameLocals,
                    templateLexicalScope,
                    parentContext,
                    isRunTimeBlock: parentContext != null );
            }

            /// <summary>
            /// Creates a child <see cref="MetaContext"/> that inherits all compile-time/run-time semantics from
            /// the parent but has its own statement list. Used for local statement grouping without changing
            /// flow control behavior (e.g., grouping statements before adding them to the parent).
            /// </summary>
            public static MetaContext CreateStatementGroupContext( MetaContext parentContext )
            {
                return new MetaContext(
                    parentContext.StatementListVariableName,
                    parentContext._generatedCodeSymbolNameLocals,
                    parentContext._templateCodeSymbolNameLocals,
                    parentContext._templateUniqueNames,
                    parentContext );
            }

            /// <summary>
            /// Creates a child <see cref="MetaContext"/> that corresponds to a new compile-time block (lexical scope).
            /// Symbols defined in the child scope are not defined in the parent scope.
            /// </summary>
            /// <param name="parentContext">The parent context.</param>
            /// <param name="isConditionalBlock">
            /// <c>true</c> if this block is the body of a compile-time conditional (if/while/for/do);
            /// <c>false</c> for regular lexical scopes like the template body.
            /// </param>
            public static MetaContext CreateForCompileTimeBlock( MetaContext parentContext, bool isConditionalBlock = false )
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
                    parentContext._templateUniqueNames,
                    parentContext,
                    isCompileTimeConditionalBlock: isConditionalBlock );
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
                    value = SyntaxFactoryEx.WellKnownIdentifier( name );
                    this._templateCodeSymbolNameLocals.Add( symbol, value );

                    return value;
                }
            }

            public SyntaxToken GetTemplateVariableName( string hint )
            {
                var name = this._templateUniqueNames.GetUniqueIdentifier( hint + "Name" );

                return SyntaxFactoryEx.WellKnownIdentifier( name );
            }

            public SyntaxToken GetVariable( string hint )
            {
                var name = this._templateUniqueNames.GetUniqueIdentifier( hint );

                return SyntaxFactoryEx.WellKnownIdentifier( name );
            }
        }
    }
}