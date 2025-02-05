// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

// ReSharper disable MissingIndent
// ReSharper disable BadExpressionBracesIndent

namespace Metalama.Framework.Engine.Linking
{
    /// <summary>
    /// Analysis step of the linker, main goal of which is to produce LinkerAnalysisRegistry.
    /// </summary>
    internal sealed partial class LinkerAnalysisStep : AspectLinkerPipelineStep<LinkerInjectionStepOutput, LinkerAnalysisStepOutput>
    {
        private readonly ProjectServiceProvider _serviceProvider;
        private readonly SyntaxGenerationOptions _syntaxGenerationOptions;

        public LinkerAnalysisStep( in ProjectServiceProvider serviceProvider )
        {
            this._serviceProvider = serviceProvider;
            this._syntaxGenerationOptions = serviceProvider.GetRequiredService<SyntaxGenerationOptions>();
        }

        public override async Task<LinkerAnalysisStepOutput> ExecuteAsync( LinkerInjectionStepOutput input, CancellationToken cancellationToken )
        {
            /*
             * Algorithm of this step:
             *  1) Collect and resolve aspect references and add implicit references (final semantic -> first override).
             *  2) Analyze reachability of semantics through aspect references, which is a DFS starting in entry point semantics, searching through all aspect references.
             *  3) Determine inlineability of reachable semantics (based on reference count).
             *  4) Determine inlineability of aspect references in pointing to inlineable semantics:
             *      * Get all inliners that can inline the reference.
             *      * If there is no inliner, reference is not inlineable.
             *      * If there is at least one inliner, reference is inlineable.
             *      * If there are multiple inliners, select one (temporarily the first one).
             *      * The selected inliner provides the principal statement.
             *  5) Inlined semantic is a semantic that is inlineable and all aspect references pointing to it are also inlineable.
             *  6) Inlined aspect reference is a aspect reference pointing to an inlined semantic.
             *  7) Analyze bodies of inlined semantics:
             *      * Collect all return statements.
             *      * Determine whether return statements are in unconditional end-points.
             *  8) Run inlining algorithm, which is DFS starting in non-inlined semantics, searching through inlined references:
             *      a) If inlined reference's replaced statement is a return statement, body is inlined without transformation of return statements.
             *      b) If inlined reference's replaced statement is NOT a return statement, all subsequent (deeper) bodies need to have return statement transformations.
             *      c) This results in having InliningSpecification for every inlineable reference.
             *  9) Create substitution objects:
             *      a) For all inlined aspect references (InliningSubstitution).
             *      b) For all return statements that were determined to require transformation in step 8) (ReturnStatementSubstitution).
             *      c) For all implicitly returning root blocks in void methods (RootBlockSubstitution).
             *      d) For all non-inlined aspect references (AspectReferenceSubstitution).
             *  10) Create LinkerAnalysisRegistry than encapsulates all results.
             */

            var inlinerProvider = new InlinerProvider();

            var referenceResolver =
                new AspectReferenceResolver(
                    input.InjectionRegistry,
                    input.OrderedAspectLayers,
                    input.IntermediateCompilation.CompilationContext );

            var symbolReferenceFinder = new SymbolReferenceFinder(
                this._serviceProvider,
                input.IntermediateCompilation.CompilationContext );

            // TODO: This is temporary to keep event field storage alive even when not referenced. May be removed after event raise transformations are implemented.
            var overriddenEventFields = input.InjectionRegistry.GetOverriddenMembers()
                .Where( s => s is IEventSymbol eventSymbol && eventSymbol.IsEventField() == true )
                .Cast<IEventSymbol>()
                .ToArray();

            var eventFieldRaiseReferences = await GetEventFieldRaiseReferencesAsync( symbolReferenceFinder, overriddenEventFields, cancellationToken );

            var aspectReferenceCollector = new AspectReferenceCollector(
                this._serviceProvider,
                input.IntermediateCompilation,
                input.InjectionRegistry,
                referenceResolver );

            var resolvedReferencesBySource = await aspectReferenceCollector.RunAsync( cancellationToken );

            var reachabilityAnalyzer = new ReachabilityAnalyzer(
                this._serviceProvider,
                input.IntermediateCompilation.CompilationContext,
                input.InjectionRegistry,
                resolvedReferencesBySource,
                eventFieldRaiseReferences.SelectAsReadOnlyList( x => x.TargetSemantic ).Distinct().ToArray() );

            var reachableSemantics = await reachabilityAnalyzer.RunAsync( cancellationToken );

            var reachableReferencesByContainingSemantic =
                new ConcurrentDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>>(
                    IntermediateSymbolSemanticEqualityComparer<IMethodSymbol>.ForCompilation( input.IntermediateCompilation.CompilationContext ) );

            var reachableReferencesByTarget =
                new ConcurrentDictionary<AspectReferenceTarget, IReadOnlyCollection<ResolvedAspectReference>>(
                    AspectReferenceTargetEqualityComparer.ForCompilation( input.IntermediateCompilation.CompilationContext ) );

            await this.GetReachableReferencesAsync(
                resolvedReferencesBySource,
                reachableSemantics,
                reachableReferencesByContainingSemantic,
                reachableReferencesByTarget,
                cancellationToken );

            var inlineabilityAnalyzer = new InlineabilityAnalyzer(
                this._serviceProvider,
                input.IntermediateCompilation.CompilationContext,
                reachableSemantics,
                inlinerProvider,
                reachableReferencesByTarget );

            var redirectedGetOnlyAutoProperties = GetRedirectedGetOnlyAutoProperties( input.InjectionRegistry, reachableSemantics );

            var redirectedSymbols = GetRedirectedSymbols(
                input.IntermediateCompilation.CompilationContext,
                redirectedGetOnlyAutoProperties );

            var inlineableSemantics = await inlineabilityAnalyzer.GetInlineableSemanticsAsync( redirectedSymbols, cancellationToken );
            var inlineableReferences = await inlineabilityAnalyzer.GetInlineableReferencesAsync( inlineableSemantics, cancellationToken );
            var inlinedSemantics = await inlineabilityAnalyzer.GetInlinedSemanticsAsync( inlineableSemantics, inlineableReferences, cancellationToken );
            var inlinedReferences = inlineabilityAnalyzer.GetInlinedReferences( inlineableReferences, inlinedSemantics );
            var nonInlinedSemantics = reachableSemantics.Except( inlinedSemantics ).ToHashSet();

            var nonInlinedReferencesByContainingSemantic = GetNonInlinedReferences(
                input.IntermediateCompilation.CompilationContext,
                reachableReferencesByContainingSemantic,
                inlinedReferences );

            VerifyUnsupportedInlineability(
                input.InjectionRegistry,
                input.IntermediateCompilation,
                input.DiagnosticSink,
                nonInlinedSemantics,
                out var overrideTargetsWithUnsupportedNonInlinedOverrides );

            var forcefullyInitializedSymbols = GetForcefullyInitializedSymbols( input.InjectionRegistry, reachableSemantics );
            var forcefullyInitializedTypes = GetForcefullyInitializedTypes( input.IntermediateCompilation, forcefullyInitializedSymbols );

            var bodyAnalyzer = new BodyAnalyzer(
                this._serviceProvider,
                input.IntermediateCompilation,
                reachableSemantics );

            var bodyAnalysisResults = await bodyAnalyzer.RunAsync( cancellationToken );

            var inliningAlgorithm = new InliningAlgorithm(
                this._serviceProvider,
                reachableReferencesByContainingSemantic,
                reachableSemantics,
                inlinedSemantics,
                inlinedReferences,
                bodyAnalysisResults );

            var inliningSpecifications = await inliningAlgorithm.RunAsync( cancellationToken );

            var redirectedGetOnlyAutoPropertyReferences = await GetRedirectedGetOnlyAutoPropertyReferencesAsync(
                symbolReferenceFinder,
                redirectedGetOnlyAutoProperties,
                cancellationToken );

            var callerAttributeReferences =
                await GetCallerAttributeReferencesAsync(
                    input.IntermediateCompilation,
                    input.InjectionRegistry,
                    symbolReferenceFinder,
                    cancellationToken );

            var substitutionGenerator = new SubstitutionGenerator(
                this,
                input.IntermediateCompilation.CompilationContext,
                input.InjectionRegistry,
                inlinedSemantics,
                nonInlinedSemantics,
                nonInlinedReferencesByContainingSemantic,
                bodyAnalysisResults,
                inliningSpecifications,
                redirectedSymbols,
                redirectedGetOnlyAutoPropertyReferences,
                forcefullyInitializedTypes,
                eventFieldRaiseReferences,
                callerAttributeReferences );

            var substitutions = await substitutionGenerator.RunAsync( cancellationToken );

            var analysisRegistry = new LinkerAnalysisRegistry(
                input.IntermediateCompilation.CompilationContext,
                reachableSemantics,
                inlinedSemantics,
                substitutions,
                overrideTargetsWithUnsupportedNonInlinedOverrides );

            return
                new LinkerAnalysisStepOutput(
                    input.DiagnosticSink,
                    input.SourceCompilationModel,
                    input.IntermediateCompilation,
                    input.InjectionRegistry,
                    input.LateTransformationRegistry,
                    analysisRegistry,
                    input.ProjectOptions );
        }

        /// <summary>
        /// Gets symbols that are redirected to another semantic.
        /// </summary>
        private static IReadOnlyList<(IPropertySymbol PropertySymbol, IntermediateSymbolSemantic TargetSemantic)> GetRedirectedGetOnlyAutoProperties(
            LinkerInjectionRegistry injectionRegistry,
            HashSet<IntermediateSymbolSemantic> reachableSemantics )
        {
            var list = new List<(IPropertySymbol PropertySymbol, IntermediateSymbolSemantic TargetSemantic)>();

            foreach ( var semantic in reachableSemantics )
            {
                if ( injectionRegistry.IsOverrideTarget( semantic.Symbol )
                     && semantic is
                     {
                         Kind: IntermediateSymbolSemanticKind.Final,
                         Symbol: IPropertySymbol { SetMethod: null, OverriddenProperty: { } } getOnlyPropertyOverride
                     }
                     && getOnlyPropertyOverride.IsAutoProperty().GetValueOrDefault() )
                {
                    // Get-only override auto property is redirected to the last override.
                    list.Add(
                        (
                            getOnlyPropertyOverride,
                            injectionRegistry.GetLastOverride( semantic.Symbol ).ToSemantic( IntermediateSymbolSemanticKind.Default )) );
                }
            }

            return list;
        }

        private static IReadOnlyDictionary<ISymbol, IntermediateSymbolSemantic> GetRedirectedSymbols(
            CompilationContext intermediateCompilationContext,
            IReadOnlyList<(IPropertySymbol PropertySymbol, IntermediateSymbolSemantic TargetSemantic)> redirectedGetOnlyAutoProperties )
        {
            var dict = new Dictionary<ISymbol, IntermediateSymbolSemantic>( intermediateCompilationContext.SymbolComparer );

            foreach ( var redirectedProperty in redirectedGetOnlyAutoProperties )
            {
                dict.Add( redirectedProperty.PropertySymbol, redirectedProperty.TargetSemantic );
            }

            return dict;
        }

        private async Task GetReachableReferencesAsync(
            IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>> resolvedReferencesBySource,
            HashSet<IntermediateSymbolSemantic> reachableSemantics,
            ConcurrentDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>> reachableReferencesBySource,
            ConcurrentDictionary<AspectReferenceTarget, IReadOnlyCollection<ResolvedAspectReference>> reachableReferencesByTarget,
            CancellationToken cancellationToken )
        {
            var concurrentTaskRunner = this._serviceProvider.GetRequiredService<IConcurrentTaskRunner>();

            void Process( KeyValuePair<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>> pair )
            {
                // Aspect references originating in non-reachable semantics should be ignored.
                var bag = new ConcurrentQueue<ResolvedAspectReference>();

                foreach ( var reference in pair.Value )
                {
                    if ( reference.TargetKind == AspectReferenceTargetKind.EventRaiseAccessor )
                    {
                        // Temporary suppression of raise.
                        continue;
                    }

                    if ( reachableSemantics.Contains( reference.ContainingSemantic ) )
                    {
                        bag.Enqueue( reference );

                        ((ConcurrentQueue<ResolvedAspectReference>) reachableReferencesBySource.GetOrAdd(
                            reference.ContainingSemantic,
                            _ => new ConcurrentQueue<ResolvedAspectReference>() )).Enqueue( reference );

                        var target = reference.ResolvedSemantic.ToAspectReferenceTarget( reference.TargetKind );

                        ((ConcurrentQueue<ResolvedAspectReference>) reachableReferencesByTarget.GetOrAdd(
                            target,
                            _ => new ConcurrentQueue<ResolvedAspectReference>() )).Enqueue( reference );
                    }
                }

                if ( !bag.IsEmpty )
                {
                    reachableReferencesBySource[pair.Key] = bag;
                }
            }

            await concurrentTaskRunner.RunConcurrentlyAsync( resolvedReferencesBySource, Process, cancellationToken );
        }

        private static IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<ResolvedAspectReference>> GetNonInlinedReferences(
            CompilationContext intermediateCompilationContext,
            IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>> reachableReferencesBySource,
            IReadOnlyDictionary<ResolvedAspectReference, Inliner> inlinedReferences )
        {
            var result =
                new Dictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyList<ResolvedAspectReference>>(
                    IntermediateSymbolSemanticEqualityComparer<IMethodSymbol>.ForCompilation( intermediateCompilationContext ) );

            foreach ( var reachableReference in reachableReferencesBySource.Values.SelectMany( x => x ) )
            {
                if ( !inlinedReferences.ContainsKey( reachableReference ) )
                {
                    ((List<ResolvedAspectReference>) result.GetOrAdd( reachableReference.ContainingSemantic, _ => new List<ResolvedAspectReference>() )).Add(
                        reachableReference );
                }
            }

            return result;
        }

        private static void VerifyUnsupportedInlineability(
            LinkerInjectionRegistry injectionRegistry,
            PartialCompilation intermediateCompilation,
            UserDiagnosticSink diagnosticSink,
            IEnumerable<IntermediateSymbolSemantic> nonInlinedSemantics,
            out HashSet<ISymbol> overrideTargetsWithUnsupportedNonInlinedOverrides )
        {
            var overrideTargets = new HashSet<ISymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

            foreach ( var nonInlinedSemantic in nonInlinedSemantics )
            {
                if ( nonInlinedSemantic.Symbol is IPropertySymbol { Parameters.Length: > 0 } or IMethodSymbol
                    {
                        MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor
                    } )
                {
                    // We only handle indexer symbol. Accessors are also not inlineable, but we don't want three messages.
                    ISymbol overrideTarget;

                    if ( injectionRegistry.IsOverrideTarget( nonInlinedSemantic.Symbol ) )
                    {
                        switch ( nonInlinedSemantic.Kind )
                        {
                            case IntermediateSymbolSemanticKind.Final:
                            case IntermediateSymbolSemanticKind.Base when nonInlinedSemantic.Symbol.IsOverride:
                            case IntermediateSymbolSemanticKind.Base
                                when nonInlinedSemantic.Symbol.TryGetHiddenSymbol( intermediateCompilation.Compilation, out _ ):
                                // Final semantics are never inlined.
                                // Base semantics for overrides are never inlined.
                                // Base semantics for hiding indexers are never inlined.
                                continue;

                            default:
                                overrideTarget = nonInlinedSemantic.Symbol;

                                break;
                        }
                    }
                    else
                    {
                        overrideTarget = injectionRegistry.GetOverrideTarget( nonInlinedSemantic.Symbol ).AssertNotNull();
                    }

                    var sourceName =
                        injectionRegistry.GetSourceAspect( nonInlinedSemantic.Symbol )?.ShortName
                        ?? "source code";

                    if ( overrideTargets.Add( overrideTarget ) )
                    {
                        // TODO: If this message stays, it needs to be improved because non-inlining is not caused by the code, but by references.
                        diagnosticSink.Report(
                            AspectLinkerDiagnosticDescriptors.DeclarationMustBeInlined.CreateRoslynDiagnostic(
                                overrideTarget.GetDiagnosticLocation(),
                                (sourceName, overrideTarget) ) );
                    }
                }
            }

            overrideTargetsWithUnsupportedNonInlinedOverrides = overrideTargets;
        }

        private static IReadOnlyList<ISymbol> GetForcefullyInitializedSymbols(
            LinkerInjectionRegistry injectionRegistry,
            HashSet<IntermediateSymbolSemantic> reachableSemantics )
        {
            var forcefullyInitializedSymbols = new List<ISymbol>();

            foreach ( var semantic in reachableSemantics )
            {
                // Currently limited to readonly structs to avoid errors.
                if ( injectionRegistry.IsOverrideTarget( semantic.Symbol )
                     && semantic is
                     {
                         Kind: IntermediateSymbolSemanticKind.Default,
                         Symbol: { IsStatic: false, ContainingType: { TypeKind: TypeKind.Struct, IsReadOnly: true } }
                     } )
                {
                    switch ( semantic.Symbol )
                    {
                        case IPropertySymbol property when property.IsAutoProperty() == true && property.HasInitializer() != true:
                            forcefullyInitializedSymbols.Add( property );

                            break;

                        case IEventSymbol @event when @event.IsEventField() == true && @event.HasInitializer() != true:
                            forcefullyInitializedSymbols.Add( @event );

                            break;
                    }
                }
            }

            return forcefullyInitializedSymbols;
        }

        private static IReadOnlyList<ForcefullyInitializedType> GetForcefullyInitializedTypes(
            PartialCompilation intermediateCompilation,
            IReadOnlyList<ISymbol> forcefullyInitializedSymbols )
        {
            var byDeclaringType = new Dictionary<INamedTypeSymbol, List<ISymbol>>( intermediateCompilation.CompilationContext.SymbolComparer );

            foreach ( var symbol in forcefullyInitializedSymbols )
            {
                var declaringType = symbol.ContainingType;

                if ( !byDeclaringType.TryGetValue( declaringType, out var list ) )
                {
                    byDeclaringType[declaringType] = list = new List<ISymbol>();
                }

                list.Add( symbol );
            }

            var constructors =
                new Dictionary<INamedTypeSymbol, List<IntermediateSymbolSemantic<IMethodSymbol>>>( intermediateCompilation.CompilationContext.SymbolComparer );

            foreach ( var type in byDeclaringType.Keys )
            {
                foreach ( var ctor in type.Constructors )
                {
                    if ( !constructors.TryGetValue( type, out var list ) )
                    {
                        constructors[type] = list = new List<IntermediateSymbolSemantic<IMethodSymbol>>();
                    }

                    list.Add( new IntermediateSymbolSemantic<IMethodSymbol>( ctor, IntermediateSymbolSemanticKind.Default ) );
                }
            }

            return constructors.SelectAsImmutableArray( x => new ForcefullyInitializedType( x.Value, byDeclaringType[x.Key] ) );
        }

        /// <summary>
        /// Filters redirected get-only auto property references from a list of all references to an event.
        /// </summary>
        private static async Task<IReadOnlyList<IntermediateSymbolSemanticReference>> GetRedirectedGetOnlyAutoPropertyReferencesAsync(
            SymbolReferenceFinder symbolReferenceFinder,
            IReadOnlyList<(IPropertySymbol Property, IntermediateSymbolSemantic TargetSemantic)> redirectedGetOnlyAutoProperties,
            CancellationToken cancellationToken )
        {
            var list = new List<IntermediateSymbolSemanticReference>();

            var allGetOnlyAutoPropertyReferences = await symbolReferenceFinder.FindSymbolReferencesAsync(
                redirectedGetOnlyAutoProperties,
                x => (x.Property, x.Property.ContainingType),
                _ => true,
                cancellationToken );

            foreach ( var reference in allGetOnlyAutoPropertyReferences )
            {
                if ( reference.ContainingSemantic.Symbol is { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } )
                {
                    list.Add( reference );
                }
            }

            return list;
        }

        /// <summary>
        /// Finds all references to overridden event fields.
        /// </summary>
        private static async Task<IReadOnlyList<IntermediateSymbolSemanticReference>> GetEventFieldRaiseReferencesAsync(
            SymbolReferenceFinder symbolReferenceFinder,
            IReadOnlyList<IEventSymbol> overriddenEventFields,
            CancellationToken cancellationToken )
        {
            var list = new List<IntermediateSymbolSemanticReference>();

            var allEventFieldReferences =
                await symbolReferenceFinder.FindSymbolReferencesAsync(
                    overriddenEventFields,
                    x => (x, x.ContainingType),
                    _ => true,
                    cancellationToken );

            foreach ( var reference in allEventFieldReferences )
            {
                switch ( reference.ReferencingNode )
                {
                    case
                    {
                        Parent: AssignmentExpressionSyntax
                        {
                            RawKind: (int) SyntaxKind.AddAssignmentExpression or (int) SyntaxKind.SubtractAssignmentExpression
                        }
                    }:
                    case
                    {
                        Parent.Parent: AssignmentExpressionSyntax
                        {
                            RawKind: (int) SyntaxKind.AddAssignmentExpression or (int) SyntaxKind.SubtractAssignmentExpression
                        }
                    }:
                        break;

                    default:
                        // Any expression that is not add or subtract assignment (which would be reference to add or remove handler).
                        list.Add( reference );

                        break;
                }
            }

            return list;
        }

        /// <summary>
        /// Finds all references to overridden methods that have caller attributes and need to be fixed.
        /// </summary>
        private static async Task<IReadOnlyList<CallerAttributeReference>> GetCallerAttributeReferencesAsync(
            PartialCompilation intermediateCompilation,
            LinkerInjectionRegistry injectionRegistry,
            SymbolReferenceFinder symbolReferenceFinder,
            CancellationToken cancellationToken )
        {
            var referenceList = new List<CallerAttributeReference>();

            // Presume that overrides always contain the full invocation without omitted parameters.
            // TODO: Optimize. Too many allocations.
            // TODO: We don't have to search methods that are inlined directly into the final semantic (all overrides and source are inlined).
            var methodsToAnalyze =
                injectionRegistry
                    .GetOverriddenMembers()
                    .AssertEach( x => x.BelongsToCompilation( intermediateCompilation.CompilationContext ) != false )
                    .Select( x => x.ContainingType )
                    .Distinct<INamedTypeSymbol>( intermediateCompilation.CompilationContext.SymbolComparer )
                    .SelectMany(
                        x =>
                            x.GetMembers()
                                .Select(
                                    member =>
                                        member switch
                                        {
                                            IMethodSymbol method => method,
                                            IPropertySymbol => null,
                                            IEventSymbol => null,
                                            IFieldSymbol => null,
                                            INamedTypeSymbol => null,
                                            _ => throw new AssertionFailedException( $"Symbol not supported: {member}." )
                                        } )
                                .OfType<IMethodSymbol>() )
                    .Where( m => !injectionRegistry.IsOverride( m ) );

            var allContainedReferences = await symbolReferenceFinder.FindMethodInvocationsAsync( methodsToAnalyze, cancellationToken );
            var semanticModelProvider = intermediateCompilation.Compilation.GetSemanticModelProvider();

            foreach ( var reference in allContainedReferences )
            {
                if ( reference.TargetSemantic.Symbol is not IMethodSymbol methodSymbol
                     || reference.TargetSemantic.Kind != IntermediateSymbolSemanticKind.Default
                     || injectionRegistry.IsOverride( reference.TargetSemantic.Symbol ) )
                {
                    // References to non-methods or non-source semantics are skipped.
                    continue;
                }

                if ( !injectionRegistry.IsOverrideTarget( reference.ContainingSemantic.Symbol ) )
                {
                    // References from non-overridden methods are skipped. 
                    continue;
                }

                // TODO: This should be cached.
                if ( !methodSymbol.Parameters.Any( p => p.IsCallerMemberNameParameter() ) )
                {
                    // References to methods without caller attributes are skipped.
                    continue;
                }

                switch ( reference.ReferencingNode )
                {
                    case InvocationExpressionSyntax invocationExpression:
                        ProcessReference( reference, invocationExpression );

                        break;
                }
            }

            return referenceList;

            void ProcessReference( IntermediateSymbolSemanticReference reference, InvocationExpressionSyntax invocationExpression )
            {
                var semanticModel = semanticModelProvider.GetSemanticModel( reference.ReferencingNode.SyntaxTree );
                var method = (IMethodSymbol?) semanticModel.GetSymbolInfo( invocationExpression ).Symbol;

                if ( method != null )
                {
                    var referencedParameterOrdinals = new HashSet<int>();

                    var index = 0;

                    foreach ( var argument in invocationExpression.ArgumentList.Arguments )
                    {
                        if ( argument.NameColon == null )
                        {
                            referencedParameterOrdinals.Add( index );
                        }
                        else
                        {
                            var referencedParameter = (IParameterSymbol) semanticModel.GetSymbolInfo( argument.NameColon.Name ).Symbol.AssertNotNull();

                            referencedParameterOrdinals.Add( referencedParameter.Ordinal );
                        }

                        index++;
                    }

                    var parametersToFix = new List<int>();

                    foreach ( var parameter in method.Parameters )
                    {
                        if ( parameter.IsCallerMemberNameParameter() && !referencedParameterOrdinals.Contains( parameter.Ordinal ) )
                        {
                            parametersToFix.Add( parameter.Ordinal );
                        }
                    }

                    if ( parametersToFix.Count > 0 )
                    {
                        referenceList.Add(
                            new CallerAttributeReference(
                                reference.ContainingSemantic,
                                reference.ContainingSemantic.Symbol,
                                (IMethodSymbol) reference.TargetSemantic.Symbol,
                                invocationExpression,
                                parametersToFix ) );
                    }
                }
            }
        }
    }
}