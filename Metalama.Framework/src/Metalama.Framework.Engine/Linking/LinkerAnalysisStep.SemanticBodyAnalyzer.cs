// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                        switch ( semantic.Symbol.Kind )
                        {
                            case SymbolKind.Method when semantic.Symbol is IMethodSymbol methodSymbol:
                                results.GetOrAdd(
                                    semantic.ToTyped<IMethodSymbol>(),
                                    static ( _, ctx ) => ctx.me.Analyze( ctx.methodSymbol ),
                                    (me: this, methodSymbol) );

                                break;

                            case SymbolKind.Property when semantic.Symbol is IPropertySymbol propertySymbol:
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

                            case SymbolKind.Event when semantic.Symbol is IEventSymbol eventSymbol:
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

                            case SymbolKind.Field:
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
            var declaration = symbol.GetPrimaryDeclarationSyntax()
                              ?? symbol.ContainingType?.GetPrimaryDeclarationSyntax();

            Invariant.Assert( declaration != null );
            var semanticModel = this._semanticModelProvider.GetSemanticModel( declaration.SyntaxTree );

            var body = GetDeclarationBody( declaration );

            switch ( body.Kind() )
            {
                case SyntaxKind.Block when body is BlockSyntax rootBlock:
                    var rootBlockCfa = semanticModel.AnalyzeControlFlow( rootBlock );
                    var exitFlowingStatements = new HashSet<StatementSyntax>();
                    var returnStatementProperties = new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>();

                    var blocksWithReturnBeforeUsingLocal = GetBlocksWithReturnBeforeUsingLocal( rootBlock, rootBlockCfa!.ReturnStatements );

                    // Get all statements that flow to exit (blocks, ifs, trys, etc.).
                    DiscoverExitFlowingStatements( rootBlock, exitFlowingStatements );

                    // Go through all return statements.
                    foreach ( var returnStatement in rootBlockCfa.ReturnStatements.OfType<ReturnStatementSyntax>() )
                    {
                        switch ( returnStatement.Parent?.Kind() )
                        {
                            case SyntaxKind.Block when returnStatement.Parent is BlockSyntax parentBlock:
                                AddIfExitFlowing( parentBlock, false, GetLastFlowStatement( parentBlock.Statements ) != returnStatement );

                                break;

                            case SyntaxKind.IfStatement when returnStatement.Parent is IfStatementSyntax ifStatement:
                                AddIfExitFlowing( ifStatement, false, false );

                                break;

                            case SyntaxKind.ElseClause when returnStatement.Parent is ElseClauseSyntax { Parent: IfStatementSyntax ifStatement }:
                                AddIfExitFlowing( ifStatement, false, false );

                                break;

                            case SyntaxKind.SwitchSection when returnStatement.Parent is SwitchSectionSyntax
                            {
                                Parent: SwitchStatementSyntax switchStatement
                            } switchSection:
                                AddIfExitFlowing( switchStatement, true, GetLastFlowStatement( switchSection.Statements ) != returnStatement );

                                break;

                            case SyntaxKind.LockStatement when returnStatement.Parent is LockStatementSyntax lockStatement:
                                AddIfExitFlowing( lockStatement, false, false );

                                break;

                            case SyntaxKind.FixedStatement when returnStatement.Parent is FixedStatementSyntax fixedStatement:
                                AddIfExitFlowing( fixedStatement, false, false );

                                break;

                            case SyntaxKind.LabeledStatement when returnStatement.Parent is LabeledStatementSyntax labeledStatement:
                                AddIfExitFlowing( labeledStatement, false, false );

                                break;

                            case SyntaxKind.UsingStatement when returnStatement.Parent is UsingStatementSyntax usingStatement:
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

                case SyntaxKind.ArrowExpressionClause:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case SyntaxKind.MethodDeclaration when body is MethodDeclarationSyntax { Body: null, ExpressionBody: null }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                    or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration
                    when body is AccessorDeclarationSyntax { Body: null, ExpressionBody: null }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case SyntaxKind.VariableDeclarator when body is VariableDeclaratorSyntax { Parent.Parent: EventFieldDeclarationSyntax }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case SyntaxKind.Parameter when body is ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } }:
                    return new SemanticBodyAnalysisResult(
                        new Dictionary<ReturnStatementSyntax, ReturnStatementProperties>(),
                        false,
                        Array.Empty<BlockSyntax>() );

                case SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration when body is RecordDeclarationSyntax:
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

                switch ( statement.Kind() )
                {
                    case SyntaxKind.ReturnStatement when statement is ReturnStatementSyntax returnStatement:
                        exitFlowingStatements.Add( returnStatement );

                        break;

                    case SyntaxKind.Block when statement is BlockSyntax block:
                        exitFlowingStatements.Add( block );

                        ProcessStatementList( block.Statements );

                        break;

                    case SyntaxKind.IfStatement when statement is IfStatementSyntax ifStatement:
                        // It is necessary to track if statements because return can be directly under the if/else instead of in a block.
                        exitFlowingStatements.Add( ifStatement );

                        DiscoverExitFlowingStatements( ifStatement.Statement, exitFlowingStatements );

                        if ( ifStatement.Else != null )
                        {
                            DiscoverExitFlowingStatements( ifStatement.Else.Statement, exitFlowingStatements );
                        }

                        break;

                    case SyntaxKind.SwitchStatement when statement is SwitchStatementSyntax switchStatement:
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

                    case SyntaxKind.LockStatement when statement is LockStatementSyntax lockStatement:
                        // It is necessary to track fixed statements because return can be directly under it.
                        exitFlowingStatements.Add( lockStatement );

                        DiscoverExitFlowingStatements( lockStatement.Statement, exitFlowingStatements );

                        break;

                    case SyntaxKind.FixedStatement when statement is FixedStatementSyntax fixedStatement:
                        // It is necessary to track fixed statements because return can be directly under it.
                        exitFlowingStatements.Add( fixedStatement );

                        DiscoverExitFlowingStatements( fixedStatement.Statement, exitFlowingStatements );

                        break;

                    case SyntaxKind.CheckedStatement when statement is CheckedStatementSyntax checkedStatement:
                        DiscoverExitFlowingStatements( checkedStatement.Block, exitFlowingStatements );

                        break;

                    case SyntaxKind.LabeledStatement when statement is LabeledStatementSyntax labeledStatement:
                        // It is necessary to track labeled statements because return can be directly under it.
                        exitFlowingStatements.Add( labeledStatement );

                        DiscoverExitFlowingStatements( labeledStatement.Statement, exitFlowingStatements );

                        break;

                    case SyntaxKind.UnsafeStatement when statement is UnsafeStatementSyntax unsafeStatement:
                        DiscoverExitFlowingStatements( unsafeStatement.Block, exitFlowingStatements );

                        break;

                    case SyntaxKind.UsingStatement when statement is UsingStatementSyntax usingStatement:
                        // It is necessary to track using statements because return can be directly under it.
                        exitFlowingStatements.Add( usingStatement );

                        DiscoverExitFlowingStatements( usingStatement.Statement, exitFlowingStatements );

                        break;

                    case SyntaxKind.TryStatement when statement is TryStatementSyntax tryStatement:
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
                            if ( statements[i].Kind() != SyntaxKind.LocalFunctionStatement )
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
                return declaration.Kind() switch
                {
                    SyntaxKind.MethodDeclaration when declaration is MethodDeclarationSyntax methodDecl => methodDecl.Body
                        ?? (SyntaxNode?) methodDecl.ExpressionBody ?? methodDecl,
                    SyntaxKind.ConstructorDeclaration when declaration is ConstructorDeclarationSyntax constructorDecl => (SyntaxNode?) constructorDecl.Body
                        ?? constructorDecl.ExpressionBody.AssertNotNull(),
                    SyntaxKind.DestructorDeclaration when declaration is DestructorDeclarationSyntax destructorDecl => (SyntaxNode?) destructorDecl.Body
                        ?? destructorDecl.ExpressionBody.AssertNotNull(),
                    SyntaxKind.OperatorDeclaration when declaration is OperatorDeclarationSyntax operatorDecl => (SyntaxNode?) operatorDecl.Body
                        ?? operatorDecl.ExpressionBody.AssertNotNull(),
                    SyntaxKind.ConversionOperatorDeclaration when declaration is ConversionOperatorDeclarationSyntax conversionOperatorDecl =>
                        (SyntaxNode?) conversionOperatorDecl.Body
                        ?? conversionOperatorDecl.ExpressionBody.AssertNotNull(),
                    SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                        or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration
                        when declaration is AccessorDeclarationSyntax accessorDecl => accessorDecl.Body
                                                                                      ?? (SyntaxNode?) accessorDecl.ExpressionBody ?? accessorDecl,
                    SyntaxKind.VariableDeclarator when declaration is VariableDeclaratorSyntax declarator => declarator,
                    SyntaxKind.ArrowExpressionClause when declaration is ArrowExpressionClauseSyntax arrowExpressionClause => arrowExpressionClause,
                    SyntaxKind.Parameter when declaration is ParameterSyntax
                    {
                        Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax }
                    } recordParameter => recordParameter,
                    SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration when declaration is RecordDeclarationSyntax recordDeclaration =>
                        recordDeclaration,
                    _ => throw new AssertionFailedException( $"Unexpected node: {CSharpExtensions.Kind( declaration )}." )
                };
            }
        }

        private static StatementSyntax? GetLastFlowStatement( SyntaxList<StatementSyntax> statements )
        {
            for ( var i = statements.Count - 1; i >= 0; i-- )
            {
                switch ( statements[i].Kind() )
                {
                    case SyntaxKind.LocalFunctionStatement:
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

                    if ( statement.Kind() == SyntaxKind.LocalDeclarationStatement
                         && statement is LocalDeclarationStatementSyntax localDeclarationStatement
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