// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Linking.Substitution;
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

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Generates all substitutions required to get correct bodies for semantics during the linking step.
    /// </summary>
    private sealed class SubstitutionGenerator
    {
        private readonly LinkerAnalysisStep _parent;
        private readonly CompilationContext _intermediateCompilationContext;
        private readonly LinkerInjectionRegistry _injectionRegistry;
        private readonly HashSet<IntermediateSymbolSemantic> _nonInlinedSemantics;
        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<ResolvedAspectReference>> _nonInlinedReferences;
        private readonly IReadOnlyList<InliningSpecification> _inliningSpecifications;
        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, SemanticBodyAnalysisResult> _bodyAnalysisResults;
        private readonly IReadOnlyDictionary<ISymbol, IntermediateSymbolSemantic> _redirectedSymbols;
        private readonly IReadOnlyList<IntermediateSymbolSemantic> _additionalTransformedSemantics;
        private readonly IReadOnlyList<ForcefullyInitializedType> _forcefullyInitializedTypes;

        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<IntermediateSymbolSemanticReference>>
            _redirectedSymbolReferencesByContainingSemantic;

        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<IntermediateSymbolSemanticReference>>
            _eventFieldRaiseReferencesByContainingSemantic;

        private readonly IReadOnlyDictionary<
            IntermediateSymbolSemantic<IMethodSymbol>,
            IReadOnlyList<CallerAttributeReference>> _callerMemberReferencesByContainingSemantic;

        private readonly IConcurrentTaskRunner _concurrentTaskRunner;

        public SubstitutionGenerator(
            LinkerAnalysisStep parent,
            CompilationContext intermediateCompilationContext,
            LinkerInjectionRegistry injectionRegistry,
            HashSet<IntermediateSymbolSemantic> inlinedSemantics,
            HashSet<IntermediateSymbolSemantic> nonInlinedSemantics,
            IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<ResolvedAspectReference>> nonInlinedReferences,
            IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, SemanticBodyAnalysisResult> bodyAnalysisResults,
            IReadOnlyList<InliningSpecification> inliningSpecifications,
            IReadOnlyDictionary<ISymbol, IntermediateSymbolSemantic> redirectedSymbols,
            IReadOnlyList<IntermediateSymbolSemanticReference> redirectedSymbolReferences,
            IReadOnlyList<ForcefullyInitializedType> forcefullyInitializedTypes,
            IReadOnlyList<IntermediateSymbolSemanticReference> eventFieldRaiseReferences,
            IReadOnlyList<CallerAttributeReference> callerMemberReferences )
        {
            this._concurrentTaskRunner = parent._serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this._parent = parent;
            this._intermediateCompilationContext = intermediateCompilationContext;
            this._injectionRegistry = injectionRegistry;
            this._nonInlinedSemantics = nonInlinedSemantics;
            this._nonInlinedReferences = nonInlinedReferences;
            this._inliningSpecifications = inliningSpecifications;
            this._bodyAnalysisResults = bodyAnalysisResults;
            this._redirectedSymbols = redirectedSymbols;
            this._forcefullyInitializedTypes = forcefullyInitializedTypes;

            this._additionalTransformedSemantics =
                redirectedSymbolReferences.SelectAsReadOnlyList( x => (IntermediateSymbolSemantic) x.ContainingSemantic )
                    .Union( eventFieldRaiseReferences.SelectAsReadOnlyList( x => (IntermediateSymbolSemantic) x.ContainingSemantic ) )
                    .Except( inlinedSemantics )
                    .Distinct()
                    .ToReadOnlyList();

            this._redirectedSymbolReferencesByContainingSemantic = IndexReferenceByContainingBody(
                intermediateCompilationContext,
                redirectedSymbolReferences,
                x => x.ContainingSemantic );

            this._eventFieldRaiseReferencesByContainingSemantic = IndexReferenceByContainingBody(
                intermediateCompilationContext,
                eventFieldRaiseReferences,
                x => x.ContainingSemantic );

            this._callerMemberReferencesByContainingSemantic = IndexReferenceByContainingBody(
                intermediateCompilationContext,
                callerMemberReferences,
                x => x.ContainingSemantic );

            static IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<T>> IndexReferenceByContainingBody<T>(
                CompilationContext compilationContext,
                IReadOnlyList<T> references,
                Func<T, IntermediateSymbolSemantic<IMethodSymbol>> getContainingSemanticFunc )
            {
                var dict =
                    new Dictionary<IntermediateSymbolSemantic<IMethodSymbol>, List<T>>(
                        IntermediateSymbolSemanticEqualityComparer<IMethodSymbol>.ForCompilation( compilationContext ) );

                foreach ( var data in references )
                {
                    var containingSemantic = getContainingSemanticFunc( data );

                    if ( !dict.TryGetValue( containingSemantic, out var list ) )
                    {
                        dict[containingSemantic] = list = new List<T>();
                    }

                    list.Add( data );
                }

                return dict.ToDictionary( x => x.Key, x => (IReadOnlyList<T>) x.Value );
            }
        }

        public async Task<IReadOnlyDictionary<InliningContextIdentifier, IReadOnlyList<SyntaxNodeSubstitution>>> RunAsync( CancellationToken cancellationToken )
        {
            var substitutions = new ConcurrentDictionary<InliningContextIdentifier, ConcurrentDictionary<SyntaxNode, SyntaxNodeSubstitution>>();

            var inliningTargetNodes = this._inliningSpecifications.SelectAsReadOnlyList( x => (x.ParentContextIdentifier, ReplacedRootNode: x.ReplacedNode) )
                .ToHashSet();

            // Add substitutions to non-inlined semantics (these are always roots of inlining).
            void ProcessNonInlinedSemantic( IntermediateSymbolSemantic nonInlinedSemantic )
            {
                if ( nonInlinedSemantic.Symbol is not IMethodSymbol )
                {
                    // Skip non-body semantics.
                    return;
                }

                var nonInlinedSemanticBody = nonInlinedSemantic.ToTyped<IMethodSymbol>();
                var context = new InliningContextIdentifier( nonInlinedSemanticBody );

                // Add aspect reference substitution for all aspect references.
                if ( this._nonInlinedReferences.TryGetValue( nonInlinedSemanticBody, out var nonInlinedReferenceList ) )
                {
                    foreach ( var nonInlinedReference in nonInlinedReferenceList )
                    {
                        AddSubstitutionsForNonInlinedReference( nonInlinedReference, context );
                    }
                }

                // Add substitutions for redirected nodes.
                if ( this._redirectedSymbolReferencesByContainingSemantic.TryGetValue( nonInlinedSemanticBody, out var redirectedSymbolReference ) )
                {
                    foreach ( var reference in redirectedSymbolReference )
                    {
                        var redirectionTarget = this._redirectedSymbols[reference.TargetSemantic.Symbol];

                        AddSubstitution(
                            context,
                            new RedirectionSubstitution( this._intermediateCompilationContext, reference.ReferencingNode, redirectionTarget ) );
                    }
                }

                // Add substitutions for event field invocation references.
                if ( this._eventFieldRaiseReferencesByContainingSemantic.TryGetValue( nonInlinedSemanticBody, out var eventFieldRaiseReferences ) )
                {
                    foreach ( var reference in eventFieldRaiseReferences )
                    {
                        AddSubstitution(
                            context,
                            new EventFieldRaiseSubstitution(
                                this._intermediateCompilationContext,
                                reference.ReferencingNode,
                                (IEventSymbol) reference.TargetSemantic.Symbol ) );
                    }
                }

                // Add substitutions for caller member references.
                if ( this._callerMemberReferencesByContainingSemantic.TryGetValue( nonInlinedSemanticBody, out var callerMemberReferences ) )
                {
                    foreach ( var reference in callerMemberReferences )
                    {
                        AddSubstitution(
                            context,
                            new CallerMemberSubstitution(
                                this._intermediateCompilationContext,
                                reference.InvocationExpression,
                                reference.ReferencingOverrideTarget,
                                reference.TargetMethod,
                                reference.ParametersToFix ) );
                    }
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync(
                this._nonInlinedSemantics.Union( this._additionalTransformedSemantics ),
                ProcessNonInlinedSemantic,
                cancellationToken );

            // Add substitutions for all inlining specifications.
            void ProcessInliningSpecification( InliningSpecification inliningSpecification )
            {
                // TODO: It's weird because here we to substitute a property getter into a syntax block.

                // Add the inlining substitution itself.
                AddSubstitution(
                    inliningSpecification.ParentContextIdentifier,
                    new InliningSubstitution( this._intermediateCompilationContext, inliningSpecification ) );

                // If not simple inlining, add return statement substitutions.
                if ( !inliningSpecification.UseSimpleInlining )
                {
                    var bodyAnalysisResult = this._bodyAnalysisResults[inliningSpecification.TargetSemantic];

                    // Add substitutions of return statements contained in the inlined body.
                    foreach ( var returnStatementRecord in bodyAnalysisResult.ReturnStatements )
                    {
                        // If the return statement is target of inlining, we don't create substitution for it.
                        if ( inliningTargetNodes.Contains( (inliningSpecification.ContextIdentifier, returnStatementRecord.Key) ) )
                        {
                            continue;
                        }

                        var returnStatement = returnStatementRecord.Key;
                        var returnStatementProperties = returnStatementRecord.Value;

                        Invariant.AssertNot( !returnStatementProperties.FlowsToExitIfRewritten && inliningSpecification.ReturnLabelIdentifier == null );

                        AddSubstitution(
                            inliningSpecification.ContextIdentifier,
                            new ReturnStatementSubstitution(
                                this._intermediateCompilationContext,
                                returnStatement,
                                inliningSpecification.AspectReference.ContainingBody,
                                inliningSpecification.TargetSemantic.Symbol,
                                inliningSpecification.ReturnVariableIdentifier,
                                inliningSpecification.ReturnLabelIdentifier,
                                returnStatementProperties.ReplaceWithBreakIfOmitted ) );
                    }

                    if ( inliningSpecification.ReturnLabelIdentifier != null &&
                         this._bodyAnalysisResults.TryGetValue( inliningSpecification.TargetSemantic, out var bodyAnalysisResults ) )
                    {
                        // Add substitutions for blocks with using <type> <local>, which needs to be transformed into using statement.
                        foreach ( var block in bodyAnalysisResults.BlocksWithReturnBeforeUsingLocal )
                        {
                            AddSubstitution(
                                inliningSpecification.ContextIdentifier,
                                new BlockWithReturnBeforeUsingLocalSubstitution( this._intermediateCompilationContext, block ) );
                        }
                    }
                }

                // Add substitution that transforms original non-block body into a statement.
                if ( inliningSpecification.TargetSemantic.Kind == IntermediateSymbolSemanticKind.Default )
                {
                    var referencedSymbol = inliningSpecification.TargetSemantic.Symbol;
                    var root = LinkerSyntaxHandler.GetCanonicalRootNode( referencedSymbol, this._injectionRegistry );

                    switch ( root )
                    {
                        case not StatementSyntax:
                            AddSubstitution(
                                inliningSpecification.ContextIdentifier,
                                this.CreateOriginalBodySubstitution(
                                    root,
                                    inliningSpecification.AspectReference.ContainingBody,
                                    referencedSymbol,
                                    inliningSpecification.UseSimpleInlining,
                                    inliningSpecification.ReturnVariableIdentifier ) );

                            break;
                    }
                }

                // Add substitutions of non-inlined aspect references.
                if ( this._nonInlinedReferences.TryGetValue( inliningSpecification.TargetSemantic, out var nonInlinedReferenceList ) )
                {
                    foreach ( var nonInlinedReference in nonInlinedReferenceList )
                    {
                        AddSubstitutionsForNonInlinedReference( nonInlinedReference, inliningSpecification.ContextIdentifier );
                    }
                }

                // Add substitutions for redirected nodes.
                if ( this._redirectedSymbolReferencesByContainingSemantic.TryGetValue( inliningSpecification.TargetSemantic, out var references ) )
                {
                    foreach ( var reference in references )
                    {
                        var redirectionTarget = this._redirectedSymbols[reference.TargetSemantic.Symbol];

                        AddSubstitution(
                            inliningSpecification.ContextIdentifier,
                            new RedirectionSubstitution( this._intermediateCompilationContext, reference.ReferencingNode, redirectionTarget ) );
                    }
                }

                // Add substitutions for event field invocation references.
                if ( this._eventFieldRaiseReferencesByContainingSemantic.TryGetValue(
                        inliningSpecification.TargetSemantic,
                        out var eventFieldRaiseReferences ) )
                {
                    foreach ( var reference in eventFieldRaiseReferences )
                    {
                        AddSubstitution(
                            inliningSpecification.ContextIdentifier,
                            new EventFieldRaiseSubstitution(
                                this._intermediateCompilationContext,
                                reference.ReferencingNode,
                                (IEventSymbol) reference.TargetSemantic.Symbol ) );
                    }
                }

                // Add substitutions for caller member references.
                if ( this._callerMemberReferencesByContainingSemantic.TryGetValue( inliningSpecification.TargetSemantic, out var callerAttributeReferences )
                     && inliningSpecification.ContextIdentifier.DestinationSemantic.Kind != IntermediateSymbolSemanticKind.Final )
                {
                    // We only want to substitute when we are inlining into non-final semantic.

                    foreach ( var reference in callerAttributeReferences )
                    {
                        AddSubstitution(
                            inliningSpecification.ContextIdentifier,
                            new CallerMemberSubstitution(
                                this._intermediateCompilationContext,
                                reference.InvocationExpression,
                                reference.ReferencingOverrideTarget,
                                reference.TargetMethod,
                                reference.ParametersToFix ) );
                    }
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( this._inliningSpecifications, ProcessInliningSpecification, cancellationToken );

            void ProcessForcefullyInitializedType( ForcefullyInitializedType forcefullyInitializedType )
            {
                foreach ( var constructor in forcefullyInitializedType.Constructors )
                {
                    var context = new InliningContextIdentifier( constructor );

                    var declaration = (ConstructorDeclarationSyntax?) constructor.Symbol.GetPrimaryDeclarationSyntax();

                    if ( declaration == null )
                    {
                        // Skip implicit constructors. If needed, the constructor will be forced to be declared in the injection step.
                        continue;
                    }

                    var rootNode = declaration.Body ?? (SyntaxNode?) declaration.ExpressionBody
                        ?? throw new AssertionFailedException( $"Declaration without body: {declaration}" );

                    AddSubstitution(
                        context,
                        new ForcedInitializationSubstitution(
                            this._intermediateCompilationContext,
                            rootNode,
                            forcefullyInitializedType.InitializedSymbols ) );
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( this._forcefullyInitializedTypes, ProcessForcefullyInitializedType, cancellationToken );

            // TODO: We convert this later back to the dictionary, but for debugging it's better to have dictionary also here.
            return substitutions.ToDictionary( x => x.Key, x => x.Value.Values.ToReadOnlyList() );

            void AddSubstitutionsForNonInlinedReference( ResolvedAspectReference nonInlinedReference, InliningContextIdentifier context )
            {
                switch ( nonInlinedReference.ResolvedSemantic )
                {
                    case { Kind: IntermediateSymbolSemanticKind.Default, Symbol: IPropertySymbol property }
                        when property.IsAutoProperty() == true && this._injectionRegistry.IsOverrideTarget( property ):
                    case { Kind: IntermediateSymbolSemanticKind.Default, Symbol: IEventSymbol @event }
                        when @event.IsEventFieldIntroduction() && this._injectionRegistry.IsOverrideTarget( @event ):
                        // For default semantic of auto properties and event fields, generate substitution that redirects to the backing field..
                        AddSubstitution(
                            context,
                            new AspectReferenceBackingFieldSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case { Kind: IntermediateSymbolSemanticKind.Base, Symbol: { IsVirtual: true } baseSymbol }
                        when !this._intermediateCompilationContext.SymbolComparer.Equals(
                            nonInlinedReference.ContainingSemantic.Symbol.ContainingType,
                            baseSymbol.ContainingType ):
                    case { Kind: IntermediateSymbolSemanticKind.Base, Symbol.IsOverride: true }
                        when this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                    case { Kind: IntermediateSymbolSemanticKind.Base, Symbol: var potentiallyHidingSymbol }
                        when potentiallyHidingSymbol.TryGetHiddenSymbol( this._intermediateCompilationContext.Compilation, out _ )
                             && this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                        // Base reference to a virtual member of the parent that is not overridden.
                        // Base references to new slot or override members are rewritten to the base member call.
                        AddSubstitution(
                            context,
                            new AspectReferenceBaseSubstitution(
                                this._intermediateCompilationContext,
                                nonInlinedReference,
                                this._parent._syntaxGenerationOptions ) );

                        break;

                    case { Symbol: IPropertySymbol { Parameters.Length: > 0 } }:
                        // Indexers (and in future constructors), adds aspect parameter to the target.
                        // TODO: Currently unused because indexer inlining is not supported. See AspectReferenceParameterSubstitution in history.

                        break;

                    case { Kind: IntermediateSymbolSemanticKind.Base, Symbol: var symbol }
                        when !this._intermediateCompilationContext.SymbolComparer.IsConvertibleTo(
                            nonInlinedReference.ContainingSemantic.Symbol.ContainingType,
                            symbol.ContainingType ):
                        // Base references to a declaration in another type mean base member call.
                        AddSubstitution(
                            context,
                            new AspectReferenceBaseSubstitution(
                                this._intermediateCompilationContext,
                                nonInlinedReference,
                                this._parent._syntaxGenerationOptions ) );

                        break;

                    case { Kind: IntermediateSymbolSemanticKind.Base, Symbol: { IsOverride: true, IsSealed: false } or { IsVirtual: true } }
                        when !this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                    case { Kind: IntermediateSymbolSemanticKind.Default }
                        when this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                        // Base references to non-overridden override member is rewritten to "source" member call.
                        // Default reference to override target is rewritten to "source" member call.
                        AddSubstitution(
                            context,
                            new AspectReferenceSourceSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case { Kind: IntermediateSymbolSemanticKind.Default }
                        when !this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol )
                             && !this._injectionRegistry.IsOverride( nonInlinedReference.ResolvedSemantic.Symbol ):
                        // Default non-inlined semantics that are not override targets need no substitutions.
                        break;

                    case { Kind: IntermediateSymbolSemanticKind.Base }:
                        // Base references to other members are rewritten to "empty" member call.
                        AddSubstitution(
                            context,
                            new AspectReferenceEmptySubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    default:
                        // Everything else targets the override.
                        AddSubstitution(
                            context,
                            new AspectReferenceOverrideSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;
                }
            }

            void AddSubstitution( InliningContextIdentifier inliningContextId, SyntaxNodeSubstitution substitution )
            {
                var dictionary = substitutions.GetOrAddNew( inliningContextId );

                if ( !dictionary.TryAdd( substitution.ReplacedNode, substitution ) )
                {
                    // TODO: The item was already added, but there is no logic to cover this situation.
                    throw new AssertionFailedException( $"The substitution was already added for node {substitution.ReplacedNode}." );
                }
            }
        }

        private SyntaxNodeSubstitution CreateOriginalBodySubstitution(
            SyntaxNode root,
            IMethodSymbol referencingSymbol,
            IMethodSymbol targetSymbol,
            bool usingSimpleInlining,
            string? returnVariableIdentifier )
            => root switch
            {
                ArrowExpressionClauseSyntax arrowExpressionClause => new ExpressionBodySubstitution(
                    this._intermediateCompilationContext,
                    arrowExpressionClause,
                    referencingSymbol,
                    targetSymbol,
                    usingSimpleInlining,
                    returnVariableIdentifier ),

                MethodDeclarationSyntax { Body: null, ExpressionBody: null } emptyPartialMethod
                    => new EmptyPartialMethodSubstitution( this._intermediateCompilationContext, emptyPartialMethod, usingSimpleInlining, returnVariableIdentifier ),

                AccessorDeclarationSyntax { Body: null, ExpressionBody: null } emptyPartialAccessor
                    => new EmptyPartialAccessorSubstitution( this._intermediateCompilationContext, emptyPartialAccessor, usingSimpleInlining, returnVariableIdentifier ),

                ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } } recordParameter
                    => new RecordParameterSubstitution( this._intermediateCompilationContext, recordParameter, targetSymbol, returnVariableIdentifier ),

                _ => throw new AssertionFailedException( $"Unexpected syntax: '{root}'." ),
            };
    }
}