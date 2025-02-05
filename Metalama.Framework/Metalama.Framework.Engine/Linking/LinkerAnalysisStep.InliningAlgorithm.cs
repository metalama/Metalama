// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    private sealed class InliningAlgorithm
    {
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;

        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>>
            _aspectReferencesByContainingSemantic;

        private readonly HashSet<IntermediateSymbolSemantic> _reachableSemantics;
        private readonly HashSet<IntermediateSymbolSemantic> _inlinedSemantics;
        private readonly IReadOnlyDictionary<ResolvedAspectReference, Inliner> _inlinedReferences;
        private readonly IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, SemanticBodyAnalysisResult> _bodyAnalysisResults;

        public InliningAlgorithm(
            ProjectServiceProvider serviceProvider,
            IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>> aspectReferencesByContainingSemantic,
            HashSet<IntermediateSymbolSemantic> reachableSemantics,
            HashSet<IntermediateSymbolSemantic> inlinedSemantics,
            IReadOnlyDictionary<ResolvedAspectReference, Inliner> inlinedReferences,
            IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, SemanticBodyAnalysisResult> bodyAnalysisResults )
        {
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this._aspectReferencesByContainingSemantic = aspectReferencesByContainingSemantic;
            this._reachableSemantics = reachableSemantics;
            this._inlinedSemantics = inlinedSemantics;
            this._inlinedReferences = inlinedReferences;
            this._bodyAnalysisResults = bodyAnalysisResults;
        }

        internal async Task<IReadOnlyList<InliningSpecification>> RunAsync( CancellationToken cancellationToken )
        {
            var inliningSpecifications = new ConcurrentQueue<InliningSpecification>();

            void ProcessSemantic( IntermediateSymbolSemantic semantic )
            {
                if ( semantic.Symbol is not IMethodSymbol )
                {
                    return;
                }

                if ( this._inlinedSemantics.Contains( semantic ) )
                {
                    // Inlined semantics are not destinations of inlining.
                    return;
                }

                // Allocate labels for the whole final method. This would be a problem only if we allow inlining multiple references in one body.
                var inliningContext = new InliningAnalysisContext();

                VisitSemantic( semantic, inliningContext );
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( this._reachableSemantics, ProcessSemantic, cancellationToken );

            return inliningSpecifications.ToReadOnlyList();

            void VisitSemantic( IntermediateSymbolSemantic semantic, InliningAnalysisContext context )
            {
                // Follow edges between method groups and accessors.
                switch ( semantic.Symbol )
                {
                    case IMethodSymbol method:
                        VisitSemanticBody(
                            method.ToSemantic( semantic.Kind ),
                            method.ToSemantic( semantic.Kind ),
                            context );

                        break;

                    case IPropertySymbol property:
                        Invariant.Assert( false ); // temp.

                        if ( property.GetMethod != null! )
                        {
                            VisitSemanticBody(
                                property.GetMethod.ToSemantic( semantic.Kind ),
                                property.GetMethod.ToSemantic( semantic.Kind ),
                                context );
                        }

                        if ( property.SetMethod != null! )
                        {
                            VisitSemanticBody(
                                property.SetMethod.ToSemantic( semantic.Kind ),
                                property.SetMethod.ToSemantic( semantic.Kind ),
                                context );
                        }

                        break;

                    case IEventSymbol @event:
                        Invariant.Assert( false ); // temp.

                        VisitSemanticBody(
                            @event.AddMethod.AssertNotNull().ToSemantic( semantic.Kind ),
                            @event.AddMethod.AssertNotNull().ToSemantic( semantic.Kind ),
                            context );

                        VisitSemanticBody(
                            @event.RemoveMethod.AssertNotNull().ToSemantic( semantic.Kind ),
                            @event.RemoveMethod.AssertNotNull().ToSemantic( semantic.Kind ),
                            context );

                        break;
                }
            }

            void VisitSemanticBody(
                IntermediateSymbolSemantic<IMethodSymbol> destinationSemantic,
                IntermediateSymbolSemantic<IMethodSymbol> currentSemantic,
                InliningAnalysisContext context )
            {
                if ( !this._aspectReferencesByContainingSemantic.TryGetValue( currentSemantic, out var aspectReferences ) )
                {
                    return;
                }

                // Go through all references and recurse into inlined bodies.
                foreach ( var aspectReference in aspectReferences )
                {
                    if ( !aspectReference.HasResolvedSemanticBody )
                    {
                        continue;
                    }

                    if ( this._inlinedReferences.TryGetValue( aspectReference, out var inliner ) )
                    {
                        var targetSemantic = aspectReference.ResolvedSemanticBody;
                        var info = inliner.GetInliningAnalysisInfo( aspectReference );

                        if ( context.UsingSimpleInlining && (info.ReplacedRootNode is ReturnStatementSyntax or EqualsValueClauseSyntax
                                                             || currentSemantic.Kind == IntermediateSymbolSemanticKind.Final) )
                        {
                            // Possible cases:
                            // * Inlining of a return statement while we can do simple inlining - no rewriting of returns is required.
                            // * Inlining of in the final semantic, which is a special case.
                            Invariant.Assert( info.ReturnVariableIdentifier == null );

                            inliningSpecifications.Enqueue(
                                new InliningSpecification(
                                    destinationSemantic,
                                    context.Id,
                                    context.ParentId,
                                    aspectReference,
                                    inliner,
                                    info.ReplacedRootNode,
                                    true,
                                    false,
                                    null,
                                    null,
                                    targetSemantic ) );

                            VisitSemanticBody( destinationSemantic, targetSemantic, context.Recurse() );
                        }
                        else if ( !SymbolEqualityComparer.Default.Equals( currentSemantic.Symbol, aspectReference.ContainingBody )
                                  && info.ReplacedRootNode is ReturnStatementSyntax or EqualsValueClauseSyntax )
                        {
                            // If inlining into a local function, revert to simple inlining.
                            inliningSpecifications.Enqueue(
                                new InliningSpecification(
                                    destinationSemantic,
                                    context.Id,
                                    context.ParentId,
                                    aspectReference,
                                    inliner,
                                    info.ReplacedRootNode,
                                    true,
                                    false,
                                    null,
                                    null,
                                    targetSemantic ) );

                            VisitSemanticBody( destinationSemantic, targetSemantic, context.RecurseWithSimpleInlining() );
                        }
                        else
                        {
                            if ( !this._bodyAnalysisResults.TryGetValue( targetSemantic, out var bodyAnalysisResult ) )
                            {
                                throw new AssertionFailedException( $"No body analysis result for '{targetSemantic.Symbol}'." );
                            }

                            // Allocate return label if and only if there is a return statement, removal of which would cause change in control flow in the inlined body.
                            var returnLabelIdentifier =
                                bodyAnalysisResult.ReturnStatements.Any( s => !s.Value.FlowsToExitIfRewritten )
                                    ? context.AllocateReturnLabel()
                                    : null;

                            var returnVariableIdentifier = info.ReturnVariableIdentifier ?? context.ReturnVariableIdentifier;

                            inliningSpecifications.Enqueue(
                                new InliningSpecification(
                                    destinationSemantic,
                                    context.Id,
                                    context.ParentId,
                                    aspectReference,
                                    inliner,
                                    info.ReplacedRootNode,
                                    false,
                                    false,
                                    returnVariableIdentifier,
                                    returnLabelIdentifier,
                                    targetSemantic ) );

                            VisitSemanticBody( destinationSemantic, targetSemantic, context.RecurseWithComplexInlining( returnVariableIdentifier ) );
                        }
                    }
                }
            }
        }
    }
}