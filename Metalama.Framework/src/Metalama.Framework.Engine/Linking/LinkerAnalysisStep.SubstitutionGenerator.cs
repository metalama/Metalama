// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Linking.Substitution;
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

#pragma warning disable IDE0052

        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<IntermediateSymbolSemanticReference>>
            _backingFieldReferencesByContainingSemantic;
#pragma warning restore IDE0052

        private readonly IReadOnlyDictionary<
            IntermediateSymbolSemantic<IMethodSymbol>,
            IReadOnlyList<CallerAttributeReference>> _callerMemberReferencesByContainingSemantic;

        private readonly IReadOnlyDictionary<
            IntermediateSymbolSemantic<IMethodSymbol>,
            IReadOnlyList<ObjectCreationCallSiteReference>> _onInitializedCallSitesByContainingSemantic;

        private readonly IReadOnlyDictionary<
            ISymbol,
            IReadOnlyList<ObjectCreationCallSiteReference>> _onInitializedCallSitesByInitializerMember;

        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IEventSymbol>, EventBrokerTransformationInfo?> _eventBrokerSemanticIndex;

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
            IReadOnlyList<IntermediateSymbolSemanticReference> backingFieldReferences,
            IReadOnlyList<CallerAttributeReference> callerMemberReferences,
            IReadOnlyList<ObjectCreationCallSiteReference> onInitializedCallSites,
            IReadOnlyDictionary<IntermediateSymbolSemantic<IEventSymbol>, EventBrokerTransformationInfo?> eventBrokerSemanticIndex )
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
            this._eventBrokerSemanticIndex = eventBrokerSemanticIndex;

            // Partition OnInitialized call sites into two buckets based on the kind of their
            // containing symbol: method-body call sites drive the inlining pipeline, while
            // field/property/event-field initializer call sites are applied directly by
            // LinkerRewritingDriver when rewriting the declaration.
            var onInitializedMethodBodyCallSites = new List<ObjectCreationCallSiteReference>();
            var onInitializedInitializerCallSites = new List<ObjectCreationCallSiteReference>();

            foreach ( var reference in onInitializedCallSites )
            {
                if ( reference.ContainingSymbol is IMethodSymbol )
                {
                    onInitializedMethodBodyCallSites.Add( reference );
                }
                else
                {
                    onInitializedInitializerCallSites.Add( reference );
                }
            }

            this._additionalTransformedSemantics =
                redirectedSymbolReferences.SelectAsReadOnlyList( x => (IntermediateSymbolSemantic) x.ContainingSemantic )
                    .Union( eventFieldRaiseReferences.SelectAsReadOnlyList( x => (IntermediateSymbolSemantic) x.ContainingSemantic ) )
                    .Union(
                        onInitializedMethodBodyCallSites.SelectAsReadOnlyList(
                            x => (IntermediateSymbolSemantic) ((IMethodSymbol) x.ContainingSymbol).ToSemantic( IntermediateSymbolSemanticKind.Default ) ) )
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

            this._backingFieldReferencesByContainingSemantic = IndexReferenceByContainingBody(
                intermediateCompilationContext,
                backingFieldReferences,
                x => x.ContainingSemantic );

            this._callerMemberReferencesByContainingSemantic = IndexReferenceByContainingBody(
                intermediateCompilationContext,
                callerMemberReferences,
                x => x.ContainingSemantic );

            this._onInitializedCallSitesByContainingSemantic = IndexReferenceByContainingBody(
                intermediateCompilationContext,
                onInitializedMethodBodyCallSites,
                x => ((IMethodSymbol) x.ContainingSymbol).ToSemantic( IntermediateSymbolSemanticKind.Default ) );

            this._onInitializedCallSitesByInitializerMember = IndexInitializerReferencesByMember(
                intermediateCompilationContext,
                onInitializedInitializerCallSites );

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

            static IReadOnlyDictionary<ISymbol, IReadOnlyList<ObjectCreationCallSiteReference>> IndexInitializerReferencesByMember(
                CompilationContext compilationContext,
                IReadOnlyList<ObjectCreationCallSiteReference> references )
            {
                var dict = new Dictionary<ISymbol, List<ObjectCreationCallSiteReference>>( compilationContext.SymbolComparer );

                foreach ( var reference in references )
                {
                    if ( !dict.TryGetValue( reference.ContainingSymbol, out var list ) )
                    {
                        dict[reference.ContainingSymbol] = list = new List<ObjectCreationCallSiteReference>();
                    }

                    list.Add( reference );
                }

                return dict.ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<ObjectCreationCallSiteReference>) x.Value,
                    compilationContext.SymbolComparer );
            }
        }

        public async Task<SubstitutionGeneratorOutput> RunAsync( CancellationToken cancellationToken )
        {
            var substitutions = new ConcurrentDictionary<InliningContextIdentifier, ConcurrentDictionary<SyntaxNode, SyntaxNodeSubstitution>>();

            var inliningTargetNodes = this._inliningSpecifications.SelectAsReadOnlyList( x => (x.ParentContextIdentifier, ReplacedRootNode: x.ReplacedNode) )
                .ToHashSet();

            // Add substitutions to non-inlined semantics (these are always roots of inlining).
            void ProcessNonInlinedSemantic( IntermediateSymbolSemantic nonInlinedSemantic )
            {
                if ( nonInlinedSemantic.Symbol.Kind != SymbolKind.Method )
                {
                    // Skip non-body semantics.
                    return;
                }

                var nonInlinedSemanticBody = nonInlinedSemantic.ToTyped<IMethodSymbol>();
                var context = new InliningContextIdentifier( nonInlinedSemanticBody );

                // Collect nodes that have redirected symbol substitutions. These take priority over aspect reference
                // substitutions because the redirection mechanism (e.g., for get-only auto property overrides) is more
                // specific than the general aspect reference renaming. An aspect reference substitution targeting a
                // parent node (MemberAccessExpression) would overwrite the child redirection (IdentifierName).
                HashSet<SyntaxNode>? aspectReferenceRootNodesWithRedirectedDescendant = null;

                if ( this._redirectedSymbolReferencesByContainingSemantic.TryGetValue( nonInlinedSemanticBody, out var redirectedSymbolReference ) )
                {
                    foreach ( var reference in redirectedSymbolReference )
                    {
                        var redirectionTarget = this._redirectedSymbols[reference.TargetSemantic.Symbol];

                        AddSubstitution(
                            context,
                            new RedirectionSubstitution( this._intermediateCompilationContext, reference.ReferencingNode, redirectionTarget ) );
                    }

                    // Build a set of aspect reference root nodes that contain a redirected descendant.
                    // Instead of walking descendants of each root (O(N*M)), walk ancestors of each redirected node (O(N*depth)).
                    if ( this._nonInlinedReferences.TryGetValue( nonInlinedSemanticBody, out var referencesForIndex ) && referencesForIndex.Count > 0 )
                    {
                        var aspectReferenceRootNodes = new HashSet<SyntaxNode>( referencesForIndex.SelectAsArray( r => r.RootNode ) );

                        aspectReferenceRootNodesWithRedirectedDescendant = new HashSet<SyntaxNode>();

                        foreach ( var reference in redirectedSymbolReference )
                        {
                            foreach ( var ancestor in reference.ReferencingNode.Ancestors() )
                            {
                                if ( aspectReferenceRootNodes.Contains( ancestor ) )
                                {
                                    aspectReferenceRootNodesWithRedirectedDescendant.Add( ancestor );

                                    break;
                                }
                            }
                        }
                    }
                }

                // Add aspect reference substitution for all aspect references, skipping any whose root node
                // has a redirected descendant (the redirection already handles the renaming).
                if ( this._nonInlinedReferences.TryGetValue( nonInlinedSemanticBody, out var nonInlinedReferenceList ) )
                {
                    foreach ( var nonInlinedReference in nonInlinedReferenceList )
                    {
                        if ( aspectReferenceRootNodesWithRedirectedDescendant != null
                             && aspectReferenceRootNodesWithRedirectedDescendant.Contains( nonInlinedReference.RootNode ) )
                        {
                            // Skip this aspect reference - its target is already handled by the redirection substitution.
                            continue;
                        }

                        AddSubstitutionsForNonInlinedReference( nonInlinedReference, context );
                    }
                }

                // Add substitutions for event field invocation references.
                if ( this._eventFieldRaiseReferencesByContainingSemantic.TryGetValue( nonInlinedSemanticBody, out var eventFieldRaiseReferences ) )
                {
                    foreach ( var reference in eventFieldRaiseReferences )
                    {
                        var eventFieldBrokerInfo =
                            this._eventBrokerSemanticIndex.TryGetValue( reference.TargetSemantic.ToTyped<IEventSymbol>(), out var info )
                                ? info
                                : null;

                        if ( eventFieldBrokerInfo != null )
                        {
                            AddSubstitution(
                                context,
                                new EventRaiseBrokerCallSubstitution(
                                    this._intermediateCompilationContext,
                                    reference.ReferencingNode,
                                    reference.TargetSemantic ) );
                        }
                        else
                        {
                            AddSubstitution(
                                context,
                                new EventRaiseBackingFieldSubstitution(
                                    this._intermediateCompilationContext,
                                    reference.ReferencingNode,
                                    (IEventSymbol) reference.TargetSemantic.Symbol ) );
                        }
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

                // Add substitutions for IInitializable call site references.
                if ( this._onInitializedCallSitesByContainingSemantic.TryGetValue( nonInlinedSemanticBody, out var onInitializedCallSites ) )
                {
                    foreach ( var reference in onInitializedCallSites )
                    {
                        AddSubstitution( context, this.CreateInitializerSubstitution( reference ) );
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

                // Add substitutions for redirected nodes first, then aspect references.
                // Redirections take priority over aspect references because the redirection mechanism
                // (e.g., for get-only auto property overrides) is more specific. An aspect reference
                // substitution targeting a parent node would overwrite the child redirection.
                // This mirrors the ordering and filtering logic in ProcessNonInlinedSemantic.
                HashSet<SyntaxNode>? inlinedAspectReferenceRootNodesWithRedirectedDescendant = null;

                if ( this._redirectedSymbolReferencesByContainingSemantic.TryGetValue( inliningSpecification.TargetSemantic, out var references ) )
                {
                    foreach ( var reference in references )
                    {
                        var redirectionTarget = this._redirectedSymbols[reference.TargetSemantic.Symbol];

                        AddSubstitution(
                            inliningSpecification.ContextIdentifier,
                            new RedirectionSubstitution( this._intermediateCompilationContext, reference.ReferencingNode, redirectionTarget ) );
                    }

                    // Build a set of aspect reference root nodes that contain a redirected descendant.
                    if ( this._nonInlinedReferences.TryGetValue( inliningSpecification.TargetSemantic, out var referencesForIndex )
                         && referencesForIndex.Count > 0 )
                    {
                        var aspectReferenceRootNodes = new HashSet<SyntaxNode>( referencesForIndex.SelectAsArray( r => r.RootNode ) );

                        inlinedAspectReferenceRootNodesWithRedirectedDescendant = new HashSet<SyntaxNode>();

                        foreach ( var reference in references )
                        {
                            foreach ( var ancestor in reference.ReferencingNode.Ancestors() )
                            {
                                if ( aspectReferenceRootNodes.Contains( ancestor ) )
                                {
                                    inlinedAspectReferenceRootNodesWithRedirectedDescendant.Add( ancestor );

                                    break;
                                }
                            }
                        }
                    }
                }

                // Add substitutions of non-inlined aspect references, skipping any whose root node
                // has a redirected descendant (the redirection already handles the renaming).
                if ( this._nonInlinedReferences.TryGetValue( inliningSpecification.TargetSemantic, out var nonInlinedReferenceList ) )
                {
                    foreach ( var nonInlinedReference in nonInlinedReferenceList )
                    {
                        if ( inlinedAspectReferenceRootNodesWithRedirectedDescendant != null
                             && inlinedAspectReferenceRootNodesWithRedirectedDescendant.Contains( nonInlinedReference.RootNode ) )
                        {
                            // Skip this aspect reference - its target is already handled by the redirection substitution.
                            continue;
                        }

                        AddSubstitutionsForNonInlinedReference( nonInlinedReference, inliningSpecification.ContextIdentifier );
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
                            new EventRaiseBackingFieldSubstitution(
                                this._intermediateCompilationContext,
                                reference.ReferencingNode,
                                (IEventSymbol) reference.TargetSemantic.Symbol ) );
                    }
                }

#if ROSLYN_5_0_0_OR_GREATER

                // Add substitutions for backing field invocation references.
                if ( this._backingFieldReferencesByContainingSemantic.TryGetValue(
                        inliningSpecification.TargetSemantic,
                        out var backingFieldReferences ) )
                {
                    foreach ( var reference in backingFieldReferences )
                    {
                        AddSubstitution(
                            inliningSpecification.ContextIdentifier,
                            new PropertyBackingFieldReferenceSubstitution(
                                this._intermediateCompilationContext,
                                reference.ReferencingNode,
                                (IPropertySymbol) reference.ContainingSemantic.Symbol.AssociatedSymbol.AssertNotNull() ) );
                    }
                }
#endif

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

            // Materialize initializer substitutions: one OnInitializedCallSiteSubstitution per
            // ObjectCreationCallSiteReference bucketed under a field / event-field / property symbol.
            // These bypass the inlining pipeline and are applied directly by LinkerRewritingDriver
            // when it rewrites the declaration.
            var initializerSubstitutions = this._onInitializedCallSitesByInitializerMember.ToDictionary(
                entry => entry.Key,
                entry => (IReadOnlyList<SyntaxNodeSubstitution>) entry.Value
                    .SelectAsArray<ObjectCreationCallSiteReference, SyntaxNodeSubstitution>( this.CreateInitializerSubstitution ),
                this._intermediateCompilationContext.SymbolComparer );

            // TODO: We convert this later back to the dictionary, but for debugging it's better to have dictionary also here.
            var contextSubstitutions = substitutions.ToDictionary( x => x.Key, x => x.Value.Values.ToReadOnlyList() );

            return new SubstitutionGeneratorOutput( contextSubstitutions, initializerSubstitutions );

            void AddSubstitutionsForNonInlinedReference( ResolvedAspectReference nonInlinedReference, InliningContextIdentifier context )
            {
                switch ( nonInlinedReference )
                {
                    case { IsVirtual: true }:
                        // Any virtual reference is skipped.
                        break;

                    case
                    {
                        ResolvedSemantic.Symbol.Kind: SymbolKind.Event,
                        ContainingBody.MethodKind: MethodKind.EventAdd or MethodKind.EventRemove,
                        ContainingSemantic.Kind: IntermediateSymbolSemanticKind.Final,
                        TargetKind: AspectReferenceTargetKind.EventRaiseAccessor
                    }:
                        // References to event raise are ignored from the final semantic because they are implicitly made by adder/remover substitution.
                        break;

                    case
                    {
                        ResolvedSemantic.Symbol.Kind: SymbolKind.Event,
                        ContainingBody.MethodKind: MethodKind.EventAdd,
                        ContainingSemantic.Kind: IntermediateSymbolSemanticKind.Final,
                        TargetKind: AspectReferenceTargetKind.EventAddAccessor
                    } when nonInlinedReference.ResolvedSemantic.Symbol is IEventSymbol @event && this._injectionRegistry.HasEventRaiseOverride( @event ):

                        AddSubstitution(
                            context,
                            new EventBrokerAdderSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case
                    {
                        ResolvedSemantic.Symbol.Kind: SymbolKind.Event,
                        ContainingBody.MethodKind: MethodKind.EventRemove,
                        ContainingSemantic.Kind: IntermediateSymbolSemanticKind.Final,
                        TargetKind: AspectReferenceTargetKind.EventRemoveAccessor
                    } when nonInlinedReference.ResolvedSemantic.Symbol is IEventSymbol @event && this._injectionRegistry.HasEventRaiseOverride( @event ):

                        AddSubstitution(
                            context,
                            new EventBrokerRemoverSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    // Unified case for non-inlined references to event add/remove accessors when target has event raise overrides
                    case
                        {
                            ResolvedSemantic.Symbol.Kind: SymbolKind.Event,
                            TargetKind: AspectReferenceTargetKind.EventAddAccessor or AspectReferenceTargetKind.EventRemoveAccessor
                        } when nonInlinedReference.ResolvedSemantic.Symbol is IEventSymbol @event
                               && this._injectionRegistry.HasEventRaiseOverride( @event )
                               && this._eventBrokerSemanticIndex.TryGetValue(
                                   nonInlinedReference.ResolvedSemantic.ToTyped<IEventSymbol>(),
                                   out var proxyEventBrokerInfo )
                               && proxyEventBrokerInfo?.BrokerProxyName != null:

                        AddSubstitution(
                            context,
                            new EventBrokerProxySubstitution(
                                this._intermediateCompilationContext,
                                nonInlinedReference,
                                proxyEventBrokerInfo.BrokerProxyName ) );

                        break;

                    case
                    {
                        ContainingBody: var method,
                        ResolvedSemantic.Symbol.Kind: SymbolKind.Event,
                        TargetKind: AspectReferenceTargetKind.EventRaiseAccessor
                    } when nonInlinedReference.ResolvedSemantic.Symbol is IEventSymbol @event:
                        var eventBrokerInfo =
                            this._eventBrokerSemanticIndex.TryGetValue( nonInlinedReference.ResolvedSemantic.ToTyped<IEventSymbol>(), out var info )
                                ? info
                                : null;

                        if ( this._injectionRegistry.IsEventRaiseOverride( method ) )
                        {
                            // This is event raise override, which can reference the handler parameter.
                            var currentEventOverride = (IEventSymbol) this._injectionRegistry.GetMainOverrideForSatelliteOverride( method ).AssertNotNull();

                            var previousSemantic =
                                this._injectionRegistry.GetPrecedingSemantic( currentEventOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ) );

                            var currentEventBrokerInfo =
                                previousSemantic != null && this._eventBrokerSemanticIndex.TryGetValue( previousSemantic.Value, out var info2 )
                                    ? info2
                                    : null;

                            if ( eventBrokerInfo == currentEventBrokerInfo )
                            {
                                AddSubstitution(
                                    context,
                                    new EventRaiseHandlerCallSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                                break;
                            }
                        }

                        if ( eventBrokerInfo != null )
                        {
                            AddSubstitution(
                                context,
                                new EventRaiseBrokerCallSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );
                        }
                        else if ( this._injectionRegistry.IsOverrideTarget( @event ) || this._injectionRegistry.IsOverride( @event ) )
                        {
                            AddSubstitution(
                                context,
                                new EventRaiseBackingFieldSubstitution(
                                    this._intermediateCompilationContext,
                                    nonInlinedReference.RootNode,
                                    @event ) );
                        }
                        else
                        {
                            AddSubstitution(
                                context,
                                new EventRaiseEventFieldSubstitution(
                                    this._intermediateCompilationContext,
                                    nonInlinedReference.RootNode,
                                    @event ) );
                        }

                        break;

                    case { ResolvedSemantic: { Kind: IntermediateSymbolSemanticKind.Default, Symbol.Kind: SymbolKind.Property } }
                        when nonInlinedReference.ResolvedSemantic.Symbol is IPropertySymbol property && property.GetPropertyKind() == PropertyKind.Auto
                                                                                                     && this._injectionRegistry.IsOverrideTarget( property ):
                    case { ResolvedSemantic: { Kind: IntermediateSymbolSemanticKind.Default, Symbol.Kind: SymbolKind.Event } }
                        when nonInlinedReference.ResolvedSemantic.Symbol is IEventSymbol @event && @event.IsEventFieldIntroduction()
                                                                                                && this._injectionRegistry.IsOverrideTarget( @event ):
                        // For default semantic of auto properties and event fields, generate substitution that redirects to the backing field..
                        AddSubstitution(
                            context,
                            new AspectReferenceBackingFieldSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base, ResolvedSemantic.Symbol: { IsVirtual: true } baseSymbol }
                        when !this._intermediateCompilationContext.SymbolComparer.Equals(
                            nonInlinedReference.ContainingSemantic.Symbol.ContainingType,
                            baseSymbol.ContainingType ):
                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base, ResolvedSemantic.Symbol.IsOverride: true }
                        when this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base, ResolvedSemantic.Symbol: var potentiallyHidingSymbol }
                        when potentiallyHidingSymbol.TryGetHiddenSymbol( this._intermediateCompilationContext.Compilation, out _ )
                             && (this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol )
                                 || nonInlinedReference.ResolvedSemantic.Symbol.Kind == SymbolKind.Field):
                        // Base reference to a virtual member of the parent that is not overridden, or to a hiding member (including fields).
                        // Base references to new slot, override, or field-hiding members are rewritten to the base member call.
                        AddSubstitution(
                            context,
                            new AspectReferenceBaseSubstitution(
                                this._intermediateCompilationContext,
                                nonInlinedReference,
                                this._parent._syntaxGenerationOptions ) );

                        break;

                    case { ResolvedSemantic.Symbol.Kind: SymbolKind.Property }
                        when nonInlinedReference.ResolvedSemantic.Symbol is IPropertySymbol { Parameters.Length: > 0 }:
                        // Indexers (and in future constructors), adds aspect parameter to the target.
                        // TODO: Currently unused because indexer inlining is not supported. See AspectReferenceParameterSubstitution in history.

                        break;

                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base, ResolvedSemantic.Symbol: var symbol }
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

                    case
                        {
                            ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base,
                            ResolvedSemantic.Symbol: { IsOverride: true, IsSealed: false } or { IsVirtual: true }
                        }
                        when !this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Default }
                        when this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol ):
                        // Base references to non-overridden override member is rewritten to "source" member call.
                        // Default reference to override target is rewritten to "source" member call.
                        AddSubstitution(
                            context,
                            new AspectReferenceSourceSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case { ResolvedSemantic: { Kind: IntermediateSymbolSemanticKind.Default, Symbol.IsStatic: true } }
                        when !this._intermediateCompilationContext.SymbolComparer.Equals(
                            nonInlinedReference.OriginalSymbol.ContainingType,
                            nonInlinedReference.ResolvedSemantic.Symbol.ContainingType ):
                        // Static member redirected from a base class to an aspect-managed hiding member.
                        // The type qualifier in the syntax needs to be rewritten.
                        AddSubstitution(
                            context,
                            new AspectReferenceOverrideSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Default }
                        when !this._injectionRegistry.IsOverrideTarget( nonInlinedReference.ResolvedSemantic.Symbol )
                             && !this._injectionRegistry.IsOverride( nonInlinedReference.ResolvedSemantic.Symbol ):
                        // Default non-inlined semantics that are not override targets need no substitutions.
                        break;

                    case
                    {
                        ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base,
                        ResolvedSemantic.Symbol: IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation }
                    }:
                        // Base references to introduced ordinary methods with no base are replaced inline
                        // with an appropriate empty expression (expression context) or suppressed (statement context).
                        AddSubstitution(
                            context,
                            new AspectReferenceEmptyMethodSubstitution( this._intermediateCompilationContext, nonInlinedReference ) );

                        break;

                    case { ResolvedSemantic.Kind: IntermediateSymbolSemanticKind.Base }:
                        // Base references to other members (properties, events) are rewritten to "empty" member call.
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
                    var existingSubstitution = dictionary[substitution.ReplacedNode];

                    // A RedirectionSubstitution and an AspectReferenceOverrideSubstitution can target the same node
                    // when a constructor with initializer advice also accesses a redirected get-only auto property.
                    // The RedirectionSubstitution takes priority because it handles the backing field redirection.
                    // Note: In practice, these two substitution types target different syntax tree levels
                    // (IdentifierNameSyntax for redirection vs MemberAccessExpressionSyntax for aspect references),
                    // so this conflict handler is a defensive measure. The primary protection is the
                    // aspectReferenceRootNodesWithRedirectedDescendant filtering in ProcessNonInlinedSemantic.
                    var isExistingRedirectionNewOverride =
                        existingSubstitution is RedirectionSubstitution && substitution is AspectReferenceOverrideSubstitution;

                    var isExistingOverrideNewRedirection =
                        existingSubstitution is AspectReferenceOverrideSubstitution && substitution is RedirectionSubstitution;

                    if ( !(isExistingRedirectionNewOverride || isExistingOverrideNewRedirection) )
                    {
                        throw new AssertionFailedException(
                            $"Conflicting substitutions for node '{substitution.ReplacedNode}': " +
                            $"existing '{existingSubstitution.GetType().Name}', new '{substitution.GetType().Name}'." );
                    }

                    // Enforce RedirectionSubstitution priority: if the new substitution is a RedirectionSubstitution
                    // and the existing one is an AspectReferenceOverrideSubstitution, replace the existing entry.
                    if ( isExistingOverrideNewRedirection )
                    {
                        dictionary[substitution.ReplacedNode] = substitution;
                    }
                }
            }
        }

        private SyntaxNodeSubstitution CreateOriginalBodySubstitution(
            SyntaxNode root,
            IMethodSymbol referencingSymbol,
            IMethodSymbol targetSymbol,
            bool usingSimpleInlining,
            string? returnVariableIdentifier )
            => root.Kind() switch
            {
                SyntaxKind.ArrowExpressionClause when root is ArrowExpressionClauseSyntax arrowExpressionClause => new ExpressionBodySubstitution(
                    this._intermediateCompilationContext,
                    arrowExpressionClause,
                    referencingSymbol,
                    targetSymbol,
                    usingSimpleInlining,
                    returnVariableIdentifier ),

                SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                    or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration
                    when root is AccessorDeclarationSyntax { Body: null, ExpressionBody: null } && targetSymbol.Kind == SymbolKind.Method
                                                                                                && targetSymbol is
                                                                                                    { AssociatedSymbol.Kind: SymbolKind.Property }
                                                                                                && targetSymbol.AssociatedSymbol is IPropertySymbol property
                                                                                                && property.IsAutoProperty() == true =>
                    new PropertyImplicitAccessorSubstitution(
                        this._intermediateCompilationContext,
                        root,
                        property ),

                SyntaxKind.MethodDeclaration when root is MethodDeclarationSyntax { Body: null, ExpressionBody: null } emptyPartialMethod
                    => new EmptyPartialMethodSubstitution(
                        this._intermediateCompilationContext,
                        emptyPartialMethod,
                        usingSimpleInlining,
                        returnVariableIdentifier ),

                SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                    or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration
                    when root is AccessorDeclarationSyntax { Body: null, ExpressionBody: null } emptyPartialAccessor
                    => new EmptyPartialAccessorSubstitution(
                        this._intermediateCompilationContext,
                        emptyPartialAccessor,
                        usingSimpleInlining,
                        returnVariableIdentifier ),

                SyntaxKind.Parameter when root is ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } } recordParameter
                    => new RecordParameterSubstitution( this._intermediateCompilationContext, recordParameter, targetSymbol, returnVariableIdentifier ),

                SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration
                    => throw AspectLinkerDiagnosticDescriptors.CannotUseProceedWithSynthesizedRecordMember.CreateException( targetSymbol ),

                _ => throw new AssertionFailedException( $"Unexpected syntax: '{root}'." )
            };

        /// <summary>
        /// Materializes the <see cref="OnInitializedCallSiteSubstitution"/> instance for a single
        /// <see cref="ObjectCreationCallSiteReference"/>, selecting the right derived class depending
        /// on whether the reference is an object creation or a <c>with</c> expression.
        /// </summary>
        private OnInitializedCallSiteSubstitution CreateInitializerSubstitution( ObjectCreationCallSiteReference reference )
            => reference.IsWithExpression
                ? new OnInitializedWithExpressionSubstitution(
                    this._intermediateCompilationContext,
                    reference.ReferencingNode,
                    reference.TypeInfo )
                : new OnInitializedObjectCreationSubstitution(
                    this._intermediateCompilationContext,
                    reference.ReferencingNode,
                    reference.TypeInfo,
                    reference.ContextParamName );
    }

    /// <summary>
    /// Output of <see cref="SubstitutionGenerator.RunAsync"/>: method-body substitutions keyed by
    /// inlining context (applied via <c>SubstitutingRewriter</c>), plus initializer substitutions
    /// keyed by the containing field / event-field / property symbol (applied directly by the
    /// linker rewriting driver when it rewrites the declaration's initializer).
    /// </summary>
    private sealed record SubstitutionGeneratorOutput(
        IReadOnlyDictionary<InliningContextIdentifier, IReadOnlyList<SyntaxNodeSubstitution>> ContextSubstitutions,
        IReadOnlyDictionary<ISymbol, IReadOnlyList<SyntaxNodeSubstitution>> InitializerSubstitutions );
}