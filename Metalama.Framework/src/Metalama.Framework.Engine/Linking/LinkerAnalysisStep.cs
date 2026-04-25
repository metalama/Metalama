// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Linking.Substitution;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.RunTime.Events;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;
using SpecialType = Metalama.Framework.Code.SpecialType;
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
            var typeMemberIdentifierGenerator = new TypeMemberIdentifierGenerator( input.IntermediateCompilation.CompilationContext );

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
                .Where( s => s.Kind == SymbolKind.Event && s is IEventSymbol eventSymbol && eventSymbol.IsEventField() == true )
                .Cast<IEventSymbol>()
                .ToArray();

            var eventFieldRaiseReferences = await GetEventFieldRaiseReferencesAsync( symbolReferenceFinder, overriddenEventFields, cancellationToken );

            var aspectReferenceCollector = new AspectReferenceCollector(
                this._serviceProvider,
                input.IntermediateCompilation,
                input.InjectionRegistry,
                referenceResolver );

            var resolvedReferencesBySource = await aspectReferenceCollector.RunAsync( cancellationToken );

            // Constructors with inserted initializer statements need to be non-discardable,
            // so their aspect references (invokers in AddInitializer templates) are properly resolved.
            // Primary constructors are excluded because their initializer statements live in an auxiliary body
            // (an injected member), not in the primary constructor's class/record declaration.
            var constructorSemantics = input.InjectionRegistry.GetConstructorsWithInsertedStatements()
                .Where( c => c.GetPrimaryDeclarationSyntax() is ConstructorDeclarationSyntax )
                .Select( c => (IntermediateSymbolSemantic) c.ToSemantic( IntermediateSymbolSemanticKind.Default ) )
                .ToArray();

            var additionalNonDiscardableSemantics = eventFieldRaiseReferences
                .SelectAsReadOnlyList( x => x.TargetSemantic )
                .Concat( constructorSemantics )
                .Distinct()
                .ToArray();

            var reachabilityAnalyzer = new ReachabilityAnalyzer(
                this._serviceProvider,
                input.IntermediateCompilation.CompilationContext,
                input.InjectionRegistry,
                resolvedReferencesBySource,
                additionalNonDiscardableSemantics );

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
                input.InjectionRegistry,
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
                input.SourceCompilationModel.CompilationContext,
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

            var overriddenHybridAutoProperties = input.InjectionRegistry.GetOverriddenMembers()
                .Where(
                    s => s.Kind == SymbolKind.Property && s is IPropertySymbol propertySymbol && propertySymbol.IsAutoProperty() == true
                         && propertySymbol.HasBody() == true )
                .Cast<IPropertySymbol>()
                .ToArray();

            var redirectedGetOnlyAutoPropertyReferences = await GetRedirectedGetOnlyAutoPropertyReferencesAsync(
                symbolReferenceFinder,
                redirectedGetOnlyAutoProperties,
                cancellationToken );

            var backingFieldReferences =
#if ROSLYN_5_0_0_OR_GREATER
                await this.GetPropertyBackingFieldReferencesAsync(
                    overriddenHybridAutoProperties,
                    cancellationToken );
#else
                Array.Empty<IntermediateSymbolSemanticReference>();
#endif

            var callerAttributeReferences =
                await GetCallerAttributeReferencesAsync(
                    input.IntermediateCompilation,
                    input.InjectionRegistry,
                    symbolReferenceFinder,
                    cancellationToken );

            CollectEventBrokerInfo(
                input.InputCompilationModel,
                input.IntermediateCompilation.CompilationContext,
                input.InjectionRegistry,
                typeMemberIdentifierGenerator,
                out var typeEventBrokers,
                out var staticDelegates );

            var eventBrokerSemanticIndex =
                BuildEventBrokerSemanticIndex(
                    input.IntermediateCompilation.CompilationContext,
                    input.InjectionRegistry,
                    typeEventBrokers );

            // Find call sites for types implementing IInitializable. The walker is expensive — it visits every
            // syntax node of every tree in the intermediate compilation — so we short-circuit it whenever we
            // can prove that no IInitializable implementer is reachable in the compilation closure.
            //
            // The closure is split into two disjoint scopes, and the OR below covers both:
            //
            //   1. Referenced assemblies — covered by CallSiteAdviceInfo.ReferencesContainInitializableTypes.
            //      This flag is aggregated by TransitivePipelineContributorSource from each referenced
            //      assembly's TransitiveAspectsManifest.ContainsInitializableTypes (written by
            //      CompileTimeAspectPipeline when the assembly was built). GetDerivedTypes cannot be used
            //      for this scope because CompilationModel.GetDerivedTypes delegates to
            //      DerivedTypeIndex.GetDerivedTypesInCurrentCompilation, which filters out types not
            //      contained in the current compilation's assembly by design.
            //
            //   2. The current compilation — covered by GetDerivedTypes(IInitializable).Any(). This is an O(1)
            //      DerivedTypeIndex dictionary lookup that includes source types *and* aspect-introduced types
            //      (the index is rebuilt on the post-aspect compilation model).
            //
            // The two scopes must both be checked: a project with no IInitializable implementer of its own
            // can still `new T()` a type declared in a referenced library that implements IInitializable,
            // and a project with no Metalama references can still contain its own implementers.
            //
            // IsPartial is a correctness escape hatch for design-time scenarios that reach the linker (today:
            // preview via PreviewAspectPipeline). In those scenarios the compilation model is backed by a
            // *partial* PartialCompilation (PartialImpl) containing only the previewed tree plus its tracked
            // dependencies, so its DerivedTypeIndex is built from a subset of the trees. But
            // OnInitializedCallSiteFinder iterates the full Roslyn Compilation.SyntaxTrees, so implementers
            // declared in trees outside the partial closure would be missed by GetDerivedTypes even though
            // the walker still sees (and must wrap) their call sites. Preview is a rare interactive operation
            // where the walker cost is irrelevant compared to correctness, so we simply force the walker to
            // run whenever the compilation is partial. This also means the design-time pipeline does not need
            // to track the flag at all: DesignTimeAspectPipelineResult.ContainsInitializableTypes returns a
            // conservative `true` for any cross-project design-time consumer.
            IReadOnlyList<ObjectCreationCallSiteReference> onInitializedCallSites;

            var closureContainsInitializable =
                input.IntermediateCompilation.IsPartial
                || input.CallSiteAdviceInfo.ReferencesContainInitializableTypes
                || input.InputCompilationModel.GetDerivedTypes( typeof(IInitializable) ).Any();

            if ( closureContainsInitializable )
            {
                var initializableTypeRegistry = new InitializableTypeRegistry( input.IntermediateCompilation.CompilationContext );

                onInitializedCallSites = await new OnInitializedCallSiteFinder(
                        this._serviceProvider,
                        input.IntermediateCompilation.CompilationContext,
                        initializableTypeRegistry )
                    .FindCallSitesAsync( cancellationToken );
            }
            else
            {
                onInitializedCallSites = Array.Empty<ObjectCreationCallSiteReference>();
            }

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
                backingFieldReferences,
                callerAttributeReferences,
                onInitializedCallSites,
                eventBrokerSemanticIndex );

            var substitutionGeneratorOutput = await substitutionGenerator.RunAsync( cancellationToken );

            var analysisRegistry = new LinkerAnalysisRegistry(
                input.IntermediateCompilation.CompilationContext,
                reachableSemantics,
                inlinedSemantics,
                substitutionGeneratorOutput.ContextSubstitutions,
                substitutionGeneratorOutput.InitializerSubstitutions,
                overrideTargetsWithUnsupportedNonInlinedOverrides,
                typeEventBrokers,
                staticDelegates,
                eventBrokerSemanticIndex );

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

        private static void CollectEventBrokerInfo(
            CompilationModel finalCompilationModel,
            CompilationContext intermediateCompilation,
            LinkerInjectionRegistry injectionRegistry,
            TypeMemberIdentifierGenerator typeMemberIdentifierGenerator,
            out IReadOnlyDictionary<IEventSymbol, EventBrokerInfo> eventBrokers,
            out IReadOnlyDictionary<INamedTypeSymbol, IReadOnlyList<StaticFieldInfo>> staticfields )
        {
            var eventBrokersWritable = new Dictionary<IEventSymbol, EventBrokerInfo>( intermediateCompilation.SymbolComparer );
            eventBrokers = eventBrokersWritable;

            var staticFieldsWritable = new Dictionary<INamedTypeSymbol, IReadOnlyList<StaticFieldInfo>>( intermediateCompilation.SymbolComparer );
            staticfields = staticFieldsWritable;

            foreach ( var injectedMember in injectionRegistry.GetInjectedMembers().Where( im => im.Semantic == InjectedMemberSemantic.OverrideEventRaise ) )
            {
                switch ( injectedMember.Transformation )
                {
                    case OverrideEventTransformation overrideEvent:
                        var targetEvent = overrideEvent.TargetDeclaration.As<IEvent>().GetTarget( finalCompilationModel );

                        var overrideMember = injectionRegistry.GetInjectedMembersForTransformation( overrideEvent )
                            .Single( im => im.Semantic == InjectedMemberSemantic.Override );

                        var overrideEventSymbol = injectionRegistry.GetSymbolForInjectedMember( overrideMember ).AssertNotNull();

                        var overrideName = overrideMember.Syntax.Kind() switch
                        {
                            SyntaxKind.EventDeclaration when overrideMember.Syntax is EventDeclarationSyntax eventDeclaration => eventDeclaration.Identifier
                                .ValueText,
                            _ => throw new NotSupportedException( $"Unsupported syntax for event override: {overrideMember.Syntax}." )
                        };

                        var targetEventSymbol =
                            (IEventSymbol) injectionRegistry.GetIntermediateCompilationSymbol<IEventSymbol>( targetEvent )
                                .AssertNotNull()
                                .GetCanonicalDefinition();

                        var delegateType = targetEvent.Type.AssertNotNull().StripNullabilityAnnotation();
                        var invokeMethod = delegateType.Methods.OfName( "Invoke" ).Single();

                        var raiseMethodName =
                            injectedMember.Syntax.Kind() switch
                            {
                                SyntaxKind.MethodDeclaration when injectedMember.Syntax is MethodDeclarationSyntax methodDeclaration => methodDeclaration
                                    .Identifier.ValueText,
                                _ => throw new NotSupportedException( $"Unsupported syntax for event raise override: {injectedMember.Syntax}." )
                            };

                        // Generate broker proxy name for non-last overrides
                        string? brokerProxyName = null;

                        if ( !injectionRegistry.IsLastOverride( overrideEventSymbol ) )
                        {
                            brokerProxyName =
                                typeMemberIdentifierGenerator.AllocateName( targetEventSymbol.ContainingType, $"{overrideName}Brokered", IdentifierFlags.None );
                        }

                        switch ( invokeMethod )
                        {
                            case { ReturnType.SpecialType: SpecialType.Void, Parameters: var parameters }
                                when parameters.All( p => p.RefKind == RefKind.None ):
                                // Delegate with RefKind.None parameters and void return type.

                                var staticDelegatesForType = (List<StaticFieldInfo>) staticFieldsWritable.GetOrAdd(
                                    targetEventSymbol.ContainingType,
                                    _ => new List<StaticFieldInfo>() );

                                var argsType = finalCompilationModel.Factory.CreateTupleType( invokeMethod.Parameters );

                                var stateType = targetEventSymbol.IsStatic
                                    ? finalCompilationModel.Factory.GetTypeByReflectionType( typeof(None) )
                                    : targetEvent.DeclaringType;

                                var stateTypeSymbol = (INamedTypeSymbol) stateType.GetSymbol().AssertSymbolNotNull();

                                if ( !eventBrokersWritable.TryGetValue( targetEventSymbol, out var eventBrokerInfo ) )
                                {
                                    var eventBrokerType =
                                        finalCompilationModel.Factory.GetNamedTypeByReflectionType( typeof(EventBroker<,,>) )
                                            .WithTypeArguments( delegateType, argsType, stateType );

                                    var eventBrokerTypeSymbol =
                                        injectionRegistry.GetIntermediateCompilationSymbol<INamedTypeSymbol>( eventBrokerType ).AssertNotNull();

                                    eventBrokerInfo = new EventBrokerInfo( targetEventSymbol, eventBrokerTypeSymbol );

                                    eventBrokersWritable.Add( targetEventSymbol, eventBrokerInfo );
                                }

                                var eventBrokerTransformationsWritable =
                                    (Dictionary<ITransformation, EventBrokerTransformationInfo>) eventBrokerInfo.Transformations;

                                var brokerCallbacksType =
                                    finalCompilationModel.Factory.GetNamedTypeByReflectionType( typeof(DelegateEventAdapter<,,>) )
                                        .WithTypeArguments( delegateType, argsType, stateType );

                                var brokerCallbacksTypeSymbol =
                                    injectionRegistry.GetIntermediateCompilationSymbol<INamedTypeSymbol>( brokerCallbacksType ).AssertNotNull();

                                // ReSharper disable AccessToModifiedClosure
                                var brokerCallbacksField = new StaticFieldInfo(
                                    targetEventSymbol.ContainingType,
                                    brokerCallbacksTypeSymbol,
                                    typeMemberIdentifierGenerator.AllocateName(
                                        targetEventSymbol.ContainingType,
                                        $"{targetEvent.Name}Adapter",
                                        IdentifierFlags.AlwaysUseSuffix ),
                                    context =>
                                        GetEventBrokerCallbacksInitializationExpression(
                                            context,
                                            stateTypeSymbol,
                                            raiseMethodName,
                                            overrideName,
                                            invokeMethod.Compilation.Factory.CreateTupleType( invokeMethod.Parameters ),
                                            delegateType,
                                            targetEventSymbol.IsStatic ) );

                                staticDelegatesForType.Add( brokerCallbacksField );

                                var eventBrokerFieldName =
                                    typeMemberIdentifierGenerator.AllocateName(
                                        targetEventSymbol.ContainingType,
                                        $"{targetEvent.Name}Broker",
                                        IdentifierFlags.MakePrivateFieldName );

                                var fieldInitializationArguments = new List<ArgumentSyntax>( 4 );

                                fieldInitializationArguments.Add(
                                    Argument(
                                        null,
                                        Token( TriviaList(), SyntaxKind.RefKeyword, TriviaList( ElasticSpace ) ),
                                        EventBrokerSyntaxHelper.GetEventBrokerField( eventBrokerFieldName, targetEvent.IsStatic ) ) );

                                fieldInitializationArguments.Add( Argument( SyntaxFactoryEx.SafeIdentifierName( brokerCallbacksField.FieldName ) ) );

                                if ( !targetEvent.IsStatic )
                                {
                                    fieldInitializationArguments.Add( Argument( ThisExpression() ) );
                                }

                                var fieldInitializationExpression =
                                    ( SyntaxGenerationContext context ) =>
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                context.SyntaxGenerator.TypeSyntax(
                                                    context.CompilationContext.ReflectionMapper.GetTypeSymbol( typeof(EventBroker) ) ),
                                                IdentifierName( nameof(EventBroker.EnsureInitialized) ) ),
                                            ArgumentList( SeparatedList( fieldInitializationArguments ) ) );

                                // ReSharper restore AccessToModifiedClosure

                                eventBrokerTransformationsWritable.Add(
                                    overrideEvent,
                                    new EventBrokerTransformationInfo(
                                        eventBrokerInfo,
                                        overrideEvent,
                                        eventBrokerFieldName,
                                        fieldInitializationExpression,
                                        brokerProxyName ) );

                                break;

                            default:
                                throw new NotSupportedException( $"Unsupported delegate signature for event broker: {invokeMethod}." );
                        }

                        break;

                    default:
                        throw new NotSupportedException( $"Unsupported injected member transformation for event broker: {injectedMember.Transformation}." );
                }
            }
        }

        private static ExpressionSyntax GetEventBrokerCastDelegateInitializationExpression(
            ITupleType invokeTupleType,
            INamedType delegateType,
            SyntaxGenerationContext context )
        {
            // We must use the invoke method to get the parameter names, and not the tuple type, because the element name is not available for 1-tuples.
            var invokeMethod = delegateType.Methods.OfName( "Invoke" ).Single();
            var parameterList = SeparatedList( invokeMethod.Parameters.SelectAsArray( p => Parameter( SyntaxFactoryEx.SafeIdentifier( p.Name ) ) ) );
            var argumentList = SeparatedList( invokeMethod.Parameters.SelectAsArray( p => Argument( SyntaxFactoryEx.SafeIdentifierName( p.Name ) ) ) );

            return
                SimpleLambdaExpression(
                    TokenList( Token( TriviaList(), SyntaxKind.StaticKeyword, TriviaList( ElasticSpace ) ) ),
                    Parameter( Identifier( "b" ) ),
                    null,
                    ParenthesizedLambdaExpression(
                        ParameterList( parameterList ),
                        null,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName( "b" ),
                                IdentifierName( "Invoke" ) ),
                            ArgumentList(
                                SingletonSeparatedList( Argument( context.SyntaxGenerator.TupleExpression( invokeTupleType, argumentList, false ) ) ) ) ) ) );
        }

        private static ExpressionSyntax GetEventBrokerInvokerDelegateInitializationExpression(
            SyntaxGenerationContext context,
            INamedTypeSymbol stateType,
            string raiseMethodName,
            IType delegateType,
            IType argsType,
            bool isStatic )
        {
            TypeSyntax? handlerTypeSyntax, stateTypeSyntax, argsTypeSyntax;

            if ( context.CompilationContext.LanguageVersion < AllLanguageVersions.CSharp14 )
            {
                // Before C# 14, we must specify the type of all lambda parameters because of `in`.
                handlerTypeSyntax = context.SyntaxGenerator.TypeSyntax( delegateType ).WithRequiredTrailingSpace();
                stateTypeSyntax = context.SyntaxGenerator.TypeSyntax( stateType ).WithRequiredTrailingSpace();
                argsTypeSyntax = context.SyntaxGenerator.TypeSyntax( argsType ).WithRequiredTrailingSpace();
            }
            else
            {
                handlerTypeSyntax = null;
                stateTypeSyntax = null;
                argsTypeSyntax = null;
            }

            var parameters = ParameterList(
                SeparatedList<ParameterSyntax>(
                [
                    Parameter( default, default, handlerTypeSyntax, Identifier( "handler" ), null ),
                    Parameter(
                        default,
                        SyntaxTokenList.Create( Token( default, SyntaxKind.RefKeyword, SyntaxFactoryEx.ElasticSpaceTriviaList ) ),
                        argsTypeSyntax,
                        Identifier( "args" ),
                        null ),
                    Parameter( default, default, stateTypeSyntax, isStatic ? SyntaxFactoryEx.DiscardIdentifier() : Identifier( "me" ), null )
                ] ) );

            ExpressionSyntax raiseMethod = isStatic
                ? SyntaxFactoryEx.SafeIdentifierName( raiseMethodName )
                : MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactoryEx.WellKnownIdentifierName( "me" ),
                    SyntaxFactoryEx.SafeIdentifierName( raiseMethodName ) );

            var expression = InvocationExpression(
                raiseMethod,
                ArgumentList(
                    SeparatedList<ArgumentSyntax>(
                    [
                        Argument( SyntaxFactoryEx.WellKnownIdentifierName( "handler" ) ),
                        Argument(
                            null,
                            Token( default, SyntaxKind.RefKeyword, SyntaxFactoryEx.ElasticSpaceTriviaList ),
                            SyntaxFactoryEx.WellKnownIdentifierName( "args" ) )
                    ] ) ) );

            return
                ParenthesizedLambdaExpression(
                    TokenList( Token( SyntaxKind.StaticKeyword ) ),
                    parameters,
                    null,
                    expression );
        }

        private static ExpressionSyntax GetEventBrokerEventAccessDelegateInitializationExpression(
            SyntaxKind operationKind,
            string overrideMethodName,
            bool isStatic )
        {
            var parameters = ParameterList(
                SeparatedList<ParameterSyntax>(
                [
                    Parameter( Identifier( "handler" ) ),
                    Parameter( isStatic ? SyntaxFactoryEx.DiscardIdentifier() : Identifier( "me" ) )
                ] ) );

            ExpressionSyntax overrideMethod = isStatic
                ? SyntaxFactoryEx.SafeIdentifierName( overrideMethodName )
                : MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactoryEx.WellKnownIdentifierName( "me" ),
                    SyntaxFactoryEx.SafeIdentifierName( overrideMethodName ) );

            var expression = AssignmentExpression(
                operationKind,
                overrideMethod,
                SyntaxFactoryEx.WellKnownIdentifierName( "handler" ) );

            return
                ParenthesizedLambdaExpression(
                    TokenList( Token( SyntaxKind.StaticKeyword ) ),
                    parameters,
                    null,
                    expression );
        }

        private static ExpressionSyntax GetEventBrokerCallbacksInitializationExpression(
            SyntaxGenerationContext context,
            INamedTypeSymbol stateType,
            string raiseMethodName,
            string overrideName,
            ITupleType argsType,
            INamedType delegateType,
            bool isStatic )
        {
            return
                ImplicitObjectCreationExpression(
                    Token( TriviaList( context.OptionalElasticEndOfLineTriviaList ), SyntaxKind.NewKeyword, TriviaList( ElasticSpace ) ),
                    ArgumentList(
                        Token( TriviaList(), SyntaxKind.OpenParenToken, TriviaList( context.OptionalElasticEndOfLineTriviaList ) ),
                        SeparatedList<ArgumentSyntax>(
                            NodeOrTokenList(
                                Argument(
                                    GetEventBrokerInvokerDelegateInitializationExpression(
                                        context,
                                        stateType,
                                        raiseMethodName,
                                        delegateType,
                                        argsType,
                                        isStatic ) ),
                                Token( TriviaList(), SyntaxKind.CommaToken, context.OptionalElasticEndOfLineTriviaList ),
                                Argument( GetEventBrokerCastDelegateInitializationExpression( argsType, delegateType, context ) ),
                                Token( TriviaList(), SyntaxKind.CommaToken, context.OptionalElasticEndOfLineTriviaList ),
                                Argument(
                                    GetEventBrokerEventAccessDelegateInitializationExpression(
                                        SyntaxKind.AddAssignmentExpression,
                                        overrideName,
                                        isStatic ) ),
                                Token( TriviaList(), SyntaxKind.CommaToken, context.OptionalElasticEndOfLineTriviaList ),
                                Argument(
                                    GetEventBrokerEventAccessDelegateInitializationExpression(
                                        SyntaxKind.SubtractAssignmentExpression,
                                        overrideName,
                                        isStatic ) ) ) ),
                        Token( TriviaList( context.OptionalElasticEndOfLineTriviaList ), SyntaxKind.CloseParenToken, TriviaList() ) ),
                    null );
        }

        private static IReadOnlyDictionary<IntermediateSymbolSemantic<IEventSymbol>, EventBrokerTransformationInfo?> BuildEventBrokerSemanticIndex(
            CompilationContext intermediateCompilationContext,
            LinkerInjectionRegistry injectionRegistry,
            IReadOnlyDictionary<IEventSymbol, EventBrokerInfo> eventBrokers )
        {
            var index =
                new Dictionary<IntermediateSymbolSemantic<IEventSymbol>, EventBrokerTransformationInfo?>(
                    IntermediateSymbolSemanticEqualityComparer<IEventSymbol>.ForCompilation( intermediateCompilationContext ) );

            foreach ( var (eventSymbol, eventBrokerInfo) in eventBrokers )
            {
                var overrides = injectionRegistry.GetOverridesForSymbol( eventSymbol );

                var semanticToBrokerMap =
                    new Dictionary<IntermediateSymbolSemantic<IEventSymbol>, EventBrokerTransformationInfo>(
                        IntermediateSymbolSemanticEqualityComparer<IEventSymbol>.ForCompilation( intermediateCompilationContext ) );

                foreach ( var @override in overrides )
                {
                    var overrideTransformation = injectionRegistry.GetTransformationForSymbol( @override ).AssertNotNull();

                    if ( eventBrokerInfo.Transformations.TryGetValue( overrideTransformation, out var eventBrokerTransformationInfo ) )
                    {
                        var overrideSemantic = ((IEventSymbol) @override).ToSemantic( IntermediateSymbolSemanticKind.Default );
                        semanticToBrokerMap.Add( overrideSemantic, eventBrokerTransformationInfo );
                    }
                }

                index.Add( eventSymbol.ToSemantic( IntermediateSymbolSemanticKind.Base ), null );
                index.Add( eventSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default ), null );

                EventBrokerTransformationInfo? currentVisibleEventBroker = null;

                for ( var i = overrides.Count - 1; i >= 0; i-- )
                {
                    if ( semanticToBrokerMap.TryGetValue(
                            ((IEventSymbol) overrides[i]).ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            out var eventBrokerTransformationInfo ) )
                    {
                        currentVisibleEventBroker = eventBrokerTransformationInfo;
                    }

                    index.Add( ((IEventSymbol) overrides[i]).ToSemantic( IntermediateSymbolSemanticKind.Default ), currentVisibleEventBroker );
                }

                index.Add( eventSymbol.ToSemantic( IntermediateSymbolSemanticKind.Final ), currentVisibleEventBroker );
            }

            return index;
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
            ConcurrentDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>>
                reachableReferencesByContainingSemantic,
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
                    if ( reachableSemantics.Contains( reference.ContainingSemantic ) )
                    {
                        bag.Enqueue( reference );

                        ((ConcurrentQueue<ResolvedAspectReference>) reachableReferencesByContainingSemantic.GetOrAdd(
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
                    reachableReferencesByContainingSemantic[pair.Key] = bag;
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
            CompilationContext sourceCompilationContext,
            UserDiagnosticSink diagnosticSink,
            IEnumerable<IntermediateSymbolSemantic> nonInlinedSemantics,
            out HashSet<ISymbol> overrideTargetsWithUnsupportedNonInlinedOverrides )
        {
            var overrideTargets = new HashSet<ISymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

            foreach ( var nonInlinedSemantic in nonInlinedSemantics )
            {
                if ( nonInlinedSemantic.Symbol.Kind is SymbolKind.Property or SymbolKind.Method
                     && ((nonInlinedSemantic.Symbol.Kind == SymbolKind.Property && nonInlinedSemantic.Symbol is IPropertySymbol { Parameters.Length: > 0 })
                         || (nonInlinedSemantic.Symbol.Kind == SymbolKind.Method && nonInlinedSemantic.Symbol is IMethodSymbol
                         {
                             MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor
                         })) )
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
                    else if ( injectionRegistry.IsOverride( nonInlinedSemantic.Symbol ) )
                    {
                        overrideTarget = injectionRegistry.GetOverrideTarget( nonInlinedSemantic.Symbol ).AssertNotNull();
                    }
                    else
                    {
                        // Source constructors with inserted initializer statements are non-inlined semantics
                        // but they are neither override targets nor overrides. Skip inlineability verification.
                        continue;
                    }

                    var sourceName =
                        injectionRegistry.GetSourceAspect( nonInlinedSemantic.Symbol )?.ShortName
                        ?? "source code";

                    if ( overrideTargets.Add( overrideTarget ) )
                    {
                        // Map the diagnostic location from the intermediate compilation to the source compilation (#818).
                        var diagnosticLocation = LinkerDiagnosticMapper.GetSourceLocation( overrideTarget, sourceCompilationContext )
                                                 ?? overrideTarget.GetDiagnosticLocation();

                        diagnosticSink.Report(
                            AspectLinkerDiagnosticDescriptors.DeclarationMustBeInlined.CreateRoslynDiagnostic(
                                diagnosticLocation,
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
                    switch ( semantic.Symbol.Kind )
                    {
                        case SymbolKind.Property when semantic.Symbol is IPropertySymbol property && property.IsAutoProperty() == true
                                                                                                  && property.HasInitializer() != true:
                            forcefullyInitializedSymbols.Add( property );

                            break;

                        case SymbolKind.Event when semantic.Symbol is IEventSymbol @event && @event.IsEventField() == true && @event.HasInitializer() != true:
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
                if ( reference.ContainingSemantic.Symbol.Kind == SymbolKind.Method && reference.ContainingSemantic.Symbol is
                    {
                        MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor
                    } )
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

#if ROSLYN_5_0_0_OR_GREATER
        /// <summary>
        /// Finds all references to auto property backing fields.
        /// </summary>
        private async Task<IReadOnlyList<IntermediateSymbolSemanticReference>> GetPropertyBackingFieldReferencesAsync(
            IReadOnlyList<IPropertySymbol> overriddenHybridAutoProperties,
            CancellationToken cancellationToken )
        {
            var concurrentTaskRunner = this._serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            var list = new ConcurrentBag<IntermediateSymbolSemanticReference>();

            await concurrentTaskRunner.RunConcurrentlyAsync(
                overriddenHybridAutoProperties,
                property =>
                {
                    var declaration = property.GetPrimaryDeclarationSyntax();

                    var (get, set) =
                        declaration?.Kind() switch
                        {
                            SyntaxKind.PropertyDeclaration when declaration is PropertyDeclarationSyntax
                                {
                                    AccessorList.Accessors:
                                    [
                                        { Keyword.RawKind: (int) SyntaxKind.GetKeyword } getAccessor,
                                        { Keyword.RawKind: (int) SyntaxKind.SetKeyword or (int) SyntaxKind.InitKeyword } setAccessor
                                    ]
                                }
                                => (getAccessor, setAccessor),
                            SyntaxKind.PropertyDeclaration when declaration is PropertyDeclarationSyntax
                                {
                                    AccessorList.Accessors:
                                    [
                                        { Keyword.RawKind: (int) SyntaxKind.SetKeyword or (int) SyntaxKind.InitKeyword } setAccessor,
                                        { Keyword.RawKind: (int) SyntaxKind.GetKeyword } getAccessor
                                    ]
                                }
                                => (getAccessor, setAccessor),
                            SyntaxKind.PropertyDeclaration when declaration is PropertyDeclarationSyntax
                                {
                                    AccessorList.Accessors: [{ Keyword.RawKind: (int) SyntaxKind.GetKeyword } getAccessor]
                                }
                                => (getAccessor, null),
                            _ => throw new InvalidOperationException( "Auto property expected." )
                        };

                    if ( get.Body != null || get.ExpressionBody != null )
                    {
                        var visitor = new AutoPropertyBodyWalker();
                        visitor.Visit( get );

                        foreach ( var fieldExpression in visitor.FieldExpressions )
                        {
                            list.Add(
                                new IntermediateSymbolSemanticReference(
                                    property.GetMethod.AssertNotNull().ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                    property.GetBackingField().AssertNotNull().ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                    fieldExpression ) );
                        }
                    }

                    if ( set?.Body != null || set?.ExpressionBody != null )
                    {
                        var visitor = new AutoPropertyBodyWalker();
                        visitor.Visit( set );

                        foreach ( var fieldExpression in visitor.FieldExpressions )
                        {
                            list.Add(
                                new IntermediateSymbolSemanticReference(
                                    property.SetMethod.AssertNotNull().ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                    property.GetBackingField().AssertNotNull().ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                    fieldExpression ) );
                        }
                    }
                },
                cancellationToken );

            return list.ToList();
        }
#endif

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
                                        member.Kind switch
                                        {
                                            SymbolKind.Method when member is IMethodSymbol method => method,
                                            SymbolKind.Property => null,
                                            SymbolKind.Event => null,
                                            SymbolKind.Field => null,
                                            SymbolKind.NamedType => null,
                                            _ => throw new AssertionFailedException( $"Symbol not supported: {member}." )
                                        } )
                                .OfType<IMethodSymbol>() )
                    .Where( m => !injectionRegistry.IsOverride( m ) );

            var allContainedReferences = await symbolReferenceFinder.FindMethodInvocationsAsync( methodsToAnalyze, cancellationToken );
            var semanticModelProvider = intermediateCompilation.Compilation.GetSemanticModelProvider();

            foreach ( var reference in allContainedReferences )
            {
                // The Kind check is already done above, so we can safely cast if Kind == Method
                if ( reference.TargetSemantic.Symbol.Kind != SymbolKind.Method
                     || reference.TargetSemantic.Kind != IntermediateSymbolSemanticKind.Default
                     || injectionRegistry.IsOverride( reference.TargetSemantic.Symbol ) )
                {
                    // References to non-methods or non-source semantics are skipped.
                    continue;
                }

                var methodSymbol = (IMethodSymbol) reference.TargetSemantic.Symbol;

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

                switch ( reference.ReferencingNode.Kind() )
                {
                    case SyntaxKind.InvocationExpression when reference.ReferencingNode is InvocationExpressionSyntax invocationExpression:
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