// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Analyzes bodies of intermediate symbol semantics.
    /// </summary>
    private sealed class BodyAnalyzer
    {
        private readonly ProjectServiceProvider _serviceProvider;
        private readonly SemanticModelProvider _semanticModelProvider;
        private readonly HashSet<IntermediateSymbolSemantic> _reachableSemantics;

        public BodyAnalyzer(
            ProjectServiceProvider serviceProvider,
            PartialCompilation intermediateCompilation,
            HashSet<IntermediateSymbolSemantic> reachableSemantics )
        {
            this._serviceProvider = serviceProvider;
            this._semanticModelProvider = intermediateCompilation.Compilation.GetSemanticModelProvider();
            this._reachableSemantics = reachableSemantics;
        }

        internal async Task<IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, SemanticBodyAnalysisResult>> RunAsync(
            CancellationToken cancellationToken )
        {
            var results = new ConcurrentDictionary<IntermediateSymbolSemantic<IMethodSymbol>, SemanticBodyAnalysisResult>();

            void AnalyzeSemantic( IntermediateSymbolSemantic semantic )
            {
                switch ( semantic.Kind )
                {
                    case IntermediateSymbolSemanticKind.Final:
                        return;

                    default:
                        switch ( semantic.Symbol )
                        {
                            case IMethodSymbol methodSymbol:
                                results.GetOrAdd(
                                    semantic.ToTyped<IMethodSymbol>(),
                                    static ( _, ctx ) => ctx.me.Analyze( ctx.methodSymbol ),
                                    (me: this, methodSymbol) );

                                break;

                            case IPropertySymbol propertySymbol:
                                if ( propertySymbol.GetMethod != null )
                                {
                                    results.GetOrAdd(
                                        semantic.WithSymbol( propertySymbol.GetMethod ),
                                        static ( _, ctx ) => ctx.me.Analyze( ctx.propertySymbol.GetMethod! ),
                                        (me: this, propertySymbol) );
                                }

                                if ( propertySymbol.SetMethod != null )
                                {
                                    results.GetOrAdd(
                                        semantic.WithSymbol( propertySymbol.SetMethod ),
                                        static ( _, ctx ) => ctx.me.Analyze( ctx.propertySymbol.SetMethod! ),
                                        (me: this, propertySymbol) );
                                }

                                break;

                            case IEventSymbol eventSymbol:
                                if ( eventSymbol.AddMethod != null )
                                {
                                    results.GetOrAdd(
                                        semantic.WithSymbol( eventSymbol.AddMethod ),
                                        static ( _, ctx ) => ctx.me.Analyze( ctx.eventSymbol.AddMethod! ),
                                        (me: this, eventSymbol) );
                                }

                                if ( eventSymbol.RemoveMethod != null )
                                {
                                    results.GetOrAdd(
                                        semantic.WithSymbol( eventSymbol.RemoveMethod ),
                                        static ( _, ctx ) => ctx.me.Analyze( ctx.eventSymbol.RemoveMethod! ),
                                        (me: this, eventSymbol) );
                                }

                                break;

                            case IFieldSymbol:
                                break;

                            default:
                                throw new AssertionFailedException( $"Unexpected symbol: '{semantic.Symbol}." );
                        }

                        break;
                }
            }

            var taskScheduler = this._serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            await taskScheduler.RunConcurrentlyAsync( this._reachableSemantics, AnalyzeSemantic, cancellationToken );

            return results;
        }

        private SemanticBodyAnalysisResult Analyze( IMethodSymbol symbol )
        {
            var declaration = symbol.GetPrimaryDeclarationSyntax().AssertNotNull();
            var semanticModel = this._semanticModelProvider.GetSemanticModel( declaration.SyntaxTree );

            var body = GetDeclarationBody( declaration );

            switch ( body )
            {
                case BlockSyntax rootBlock:
                    var rootBlockCfa = semanticModel.AnalyzeControlFlow( rootBlock );
                    var exitFlowingStatements = new HashSet<StatementSyntax>();
                    var returnStatementProperties = new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>();

                    var blocksWithReturnBeforeUsingLocal = GetBlocksWithReturnBeforeUsingLocal( rootBlock, rootBlockCfa.ReturnStatements );

                    // Get all statements that flow to exit (blocks, ifs, trys, etc.).
                    DiscoverExitFlowingStatements( rootBlock, exitFlowingStatements );

                    // Go through all return statements.
                    foreach ( var returnStatement in rootBlockCfa.ReturnStatements.OfType<ReturnStatementSyntax>() )
                    {
                        switch ( returnStatement )
                        {
                            case { Parent: BlockSyntax parentBlock }:
                                AddIfExitFlowing( parentBlock, false, GetLastFlowStatement( parentBlock.Statements ) != returnStatement );

                                break;

                            case { Parent: IfStatementSyntax ifStatement }:
                                AddIfExitFlowing( ifStatement, false, false );

                                break;

                            case { Parent: ElseClauseSyntax { Parent: IfStatementSyntax ifStatement } }:
                                AddIfExitFlowing( ifStatement, false, false );

                                break;

                            case { Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax switchStatement } switchSection }:
                                AddIfExitFlowing( switchStatement, true, GetLastFlowStatement( switchSection.Statements ) != returnStatement );

                                break;

                            case { Parent: LockStatementSyntax lockStatement }:
                                AddIfExitFlowing( lockStatement, false, false );

                                break;

                            case { Parent: FixedStatementSyntax fixedStatement }:
                                AddIfExitFlowing( fixedStatement, false, false );

                                break;

                            case { Parent: LabeledStatementSyntax labeledStatement }:
                                AddIfExitFlowing( labeledStatement, false, false );

                                break;

                            case { Parent: UsingStatementSyntax usingStatement }:
                                AddIfExitFlowing( usingStatement, false, false );

                                break;

                            default:
                                returnStatementProperties.Add( returnStatement, new ReturnStatementProperties( false, false ) );

                                break;
                        }

                        void AddIfExitFlowing( StatementSyntax controlStatement, bool replaceByBreakIfOmitted, bool followedByCode )
                        {
                            if ( exitFlowingStatements.Contains( controlStatement ) )
                            {
                                returnStatementProperties.Add( returnStatement, new ReturnStatementProperties( !followedByCode, replaceByBreakIfOmitted ) );
                            }
                            else
                            {
                                returnStatementProperties.Add( returnStatement, new ReturnStatementProperties( false, false ) );
                            }
                        }
                    }

                    return new SemanticBodyAnalysisResult( returnStatementProperties, rootBlockCfa.EndPointIsReachable, blocksWithReturnBeforeUsingLocal );

                case ArrowExpressionClauseSyntax:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case MethodDeclarationSyntax { Body: null, ExpressionBody: null }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case AccessorDeclarationSyntax { Body: null, ExpressionBody: null }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case VariableDeclaratorSyntax { Parent.Parent: EventFieldDeclarationSyntax }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                default:
                    throw new AssertionFailedException( $"Unexpected body for '{symbol}'." );
            }

            static void DiscoverExitFlowingStatements( StatementSyntax statement, HashSet<StatementSyntax> exitFlowingStatements )
            {
                /* Start with the root block or the last statement of a block.
                 * This method adds the following statements that flow to the end-point of the method:
                 *   * Blocks.
                 *   * If statements.
                 *   * Try statements.
                 *   * Switch statements.
                 *   * Lock statements.
                 *   * Fixed statements.
                 *   * Using statements.
                 *   * Labeled statements.
                 */

                switch ( statement )
                {
                    case ReturnStatementSyntax returnStatement:
                        exitFlowingStatements.Add( returnStatement );

                        break;

                    case BlockSyntax block:
                        exitFlowingStatements.Add( block );

                        ProcessStatementList( block.Statements );

                        break;

                    case IfStatementSyntax ifStatement:
                        // It is necessary to track if statements because return can be directly under the if/else instead of in a block.
                        exitFlowingStatements.Add( ifStatement );

                        DiscoverExitFlowingStatements( ifStatement.Statement, exitFlowingStatements );

                        if ( ifStatement.Else != null )
                        {
                            DiscoverExitFlowingStatements( ifStatement.Else.Statement, exitFlowingStatements );
                        }

                        break;

                    case SwitchStatementSyntax switchStatement:
                        // It is necessary to track switch statements because return can be directly under one of the sections.
                        exitFlowingStatements.Add( switchStatement );

                        foreach ( var section in switchStatement.Sections )
                        {
                            if ( section.Statements.Count > 0 )
                            {
                                ProcessStatementList( section.Statements );
                            }
                        }

                        break;

                    case LockStatementSyntax lockStatement:
                        // It is necessary to track fixed statements because return can be directly under it.
                        exitFlowingStatements.Add( lockStatement );

                        DiscoverExitFlowingStatements( lockStatement.Statement, exitFlowingStatements );

                        break;

                    case FixedStatementSyntax fixedStatement:
                        // It is necessary to track fixed statements because return can be directly under it.
                        exitFlowingStatements.Add( fixedStatement );

                        DiscoverExitFlowingStatements( fixedStatement.Statement, exitFlowingStatements );

                        break;

                    case CheckedStatementSyntax checkedStatement:
                        DiscoverExitFlowingStatements( checkedStatement.Block, exitFlowingStatements );

                        break;

                    case LabeledStatementSyntax labeledStatement:
                        // It is necessary to track labeled statements because return can be directly under it.
                        exitFlowingStatements.Add( labeledStatement );

                        DiscoverExitFlowingStatements( labeledStatement.Statement, exitFlowingStatements );

                        break;

                    case UnsafeStatementSyntax unsafeStatement:
                        DiscoverExitFlowingStatements( unsafeStatement.Block, exitFlowingStatements );

                        break;

                    case UsingStatementSyntax usingStatement:
                        // It is necessary to track using statements because return can be directly under it.
                        exitFlowingStatements.Add( usingStatement );

                        DiscoverExitFlowingStatements( usingStatement.Statement, exitFlowingStatements );

                        break;

                    case TryStatementSyntax tryStatement:
                        DiscoverExitFlowingStatements( tryStatement.Block, exitFlowingStatements );

                        foreach ( var catchClause in tryStatement.Catches )
                        {
                            DiscoverExitFlowingStatements( catchClause.Block, exitFlowingStatements );
                        }

                        if ( tryStatement.Finally != null )
                        {
                            DiscoverExitFlowingStatements( tryStatement.Finally.Block, exitFlowingStatements );
                        }

                        break;
                }

                void ProcessStatementList( IReadOnlyList<StatementSyntax> statements )
                {
                    if ( statements.Count > 0 )
                    {
                        // Only the last statement (excluding local function declarations) flows to the exit.

                        StatementSyntax? lastNonIgnoredStatement = null;

                        for ( var i = statements.Count - 1; i >= 0; i-- )
                        {
                            if ( statements[i] is not LocalFunctionStatementSyntax )
                            {
                                lastNonIgnoredStatement = statements[i];

                                break;
                            }
                        }

                        if ( lastNonIgnoredStatement != null )
                        {
                            DiscoverExitFlowingStatements( lastNonIgnoredStatement, exitFlowingStatements );
                        }
                    }
                }
            }

            static SyntaxNode GetDeclarationBody( SyntaxNode declaration )
            {
                return declaration switch
                {
                    MethodDeclarationSyntax methodDecl => methodDecl.Body ?? (SyntaxNode?) methodDecl.ExpressionBody ?? methodDecl,
                    ConstructorDeclarationSyntax constructorDecl => (SyntaxNode?) constructorDecl.Body ?? constructorDecl.ExpressionBody.AssertNotNull(),
                    DestructorDeclarationSyntax destructorDecl => (SyntaxNode?) destructorDecl.Body ?? destructorDecl.ExpressionBody.AssertNotNull(),
                    OperatorDeclarationSyntax operatorDecl => (SyntaxNode?) operatorDecl.Body ?? operatorDecl.ExpressionBody.AssertNotNull(),
                    ConversionOperatorDeclarationSyntax conversionOperatorDecl => (SyntaxNode?) conversionOperatorDecl.Body
                                                                                  ?? conversionOperatorDecl.ExpressionBody.AssertNotNull(),
                    AccessorDeclarationSyntax accessorDecl => accessorDecl.Body ?? (SyntaxNode?) accessorDecl.ExpressionBody ?? accessorDecl,
                    VariableDeclaratorSyntax declarator => declarator,
                    ArrowExpressionClauseSyntax arrowExpressionClause => arrowExpressionClause,
                    ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } } recordParameter => recordParameter,
                    _ => throw new AssertionFailedException( $"Unexpected node: {CSharpExtensions.Kind( declaration )}." )
                };
            }
        }

        private static StatementSyntax? GetLastFlowStatement( SyntaxList<StatementSyntax> statements )
        {
            for ( var i = statements.Count - 1; i >= 0; i-- )
            {
                switch ( statements[i] )
                {
                    case LocalFunctionStatementSyntax:
                        // Local function statement does not affect flow, so we ignore it.
                        continue;

                    default:
                        return statements[i];
                }
            }

            return null;
        }

        private static IReadOnlyList<BlockSyntax> GetBlocksWithReturnBeforeUsingLocal( BlockSyntax rootBlock, IReadOnlyList<SyntaxNode> returnStatements )
        {
            var statementsContainingReturnStatement = new HashSet<StatementSyntax>();

            foreach ( var returnStatement in returnStatements )
            {
                Mark( returnStatement );

                void Mark( SyntaxNode node )
                {
                    if ( node == rootBlock )
                    {
                        statementsContainingReturnStatement.Add( rootBlock );

                        return;
                    }

                    if ( node is StatementSyntax statement )
                    {
                        if ( statementsContainingReturnStatement.Add( statement ) && statement != rootBlock )
                        {
                            // Process recursively unvisited statement that is not the root block.
                            Mark( statement.Parent.AssertNotNull() );
                        }
                    }
                    else
                    {
                        // Process recursively the parent of a non-statement.
                        Mark( node.Parent.AssertNotNull() );
                    }
                }
            }

            if ( statementsContainingReturnStatement.Count == 0 )
            {
                return Array.Empty<BlockSyntax>();
            }

            var blocksWithUsingLocalAfterReturn = new List<BlockSyntax>();

            // Process every block that contained a return statement.
            foreach ( var block in statementsContainingReturnStatement.OfType<BlockSyntax>() )
            {
                var encounteredStatementContainingReturnStatement = false;

                foreach ( var statement in block.Statements )
                {
                    if ( statementsContainingReturnStatement.Contains( statement ) )
                    {
                        encounteredStatementContainingReturnStatement = true;
                    }

                    if ( statement is LocalDeclarationStatementSyntax localDeclarationStatement
                         && localDeclarationStatement.UsingKeyword != default
                         && encounteredStatementContainingReturnStatement )
                    {
                        blocksWithUsingLocalAfterReturn.Add( block );

                        break;
                    }
                }
            }

            return blocksWithUsingLocalAfterReturn;
        }
    }
}