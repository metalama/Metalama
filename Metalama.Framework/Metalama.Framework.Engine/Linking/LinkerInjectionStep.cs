// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

#if DEBUG
using Metalama.Framework.Engine.Formatting;
#endif

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Aspect linker injection steps. Adds introduced members from all transformation to the Roslyn compilation. This involves calling template expansion.
/// This results in the transformation registry and intermediate compilation, and also produces diagnostics.
/// </summary>
internal sealed partial class LinkerInjectionStep : AspectLinkerPipelineStep<AspectLinkerInput, LinkerInjectionStepOutput>
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly CompilationContext _compilationContext;
    private readonly IConcurrentTaskRunner _concurrentTaskRunner;
    private readonly SyntaxGenerationOptions _syntaxGenerationOptions;

    public LinkerInjectionStep( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
    {
        this._serviceProvider = serviceProvider;
        this._syntaxGenerationOptions = serviceProvider.GetRequiredService<SyntaxGenerationOptions>();
        this._compilationContext = compilationContext;
        this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
    }

    public override async Task<LinkerInjectionStepOutput> ExecuteAsync( AspectLinkerInput input, CancellationToken cancellationToken )
    {
        // TODO: Consider parallelization based on containing type and not syntax tree. This would remove non-determinism in name selection.

        // We don't use a code fix filter because the linker is not supposed to suggest code fixes. If that changes, we need to pass a filter.
        var diagnostics = new UserDiagnosticSink( this._serviceProvider );

        var supportsNullability = input.FinalCompilationModel.RoslynCompilation.Options.NullableContextOptions != NullableContextOptions.Disable;

        var transformationComparer = TransformationLinkerOrderComparer.Instance;
        var injectionHelperProvider = new LinkerInjectionHelperProvider( input.FinalCompilationModel, supportsNullability );
        var injectionNameProvider = new LinkerInjectionNameProvider( input.FinalCompilationModel, injectionHelperProvider );
        var transformationCollection = new TransformationCollection( input.FinalCompilationModel, transformationComparer );
        var lexicalScopeFactory = new LexicalScopeFactory( input.FinalCompilationModel );
        var aspectReferenceSyntaxProvider = new LinkerAspectReferenceSyntaxProvider();

        HashSet<IIntroduceDeclarationTransformation> replacedIntroduceDeclarationTransformations = [];

        ConcurrentDictionary<IFullRef<IMember>, InsertStatementTransformationContextImpl>
            pendingInsertStatementContexts = new( RefEqualityComparer<IMember>.Default );

        ConcurrentDictionary<IFullRef<IMember>, AuxiliaryMemberTransformations> auxiliaryMemberTransformations = new( RefEqualityComparer<IMember>.Default );

        var existingSyntaxTrees = input.FinalCompilationModel.PartialCompilation.SyntaxTrees;

        void IndexTransformationsInSyntaxTree( IGrouping<SyntaxTree, ISyntaxTreeTransformation> transformationGroup )
        {
            // Transformations need to be sorted here because some transformations require a LexicalScope to get an unique name, and it
            // will give deterministic results only when called in a deterministic order.
            var sortedTransformations = transformationGroup.OrderBy( x => x, transformationComparer ).ToArray();

            // IntroduceDeclarationTransformation instances need to be indexed first.
            foreach ( var transformation in sortedTransformations )
            {
                IndexIntroduceDeclarationTransformation(
                    existingSyntaxTrees,
                    transformation,
                    transformationCollection );
            }

            // Replace transformations need to be indexed second.
            // NOTE: This is correct because replaced transformation is always in the same syntax tree as the replacing one.
            foreach ( var transformation in sortedTransformations )
            {
                IndexReplaceTransformation(
                    transformation,
                    transformationCollection,
                    replacedIntroduceDeclarationTransformations );
            }

            foreach ( var transformation in sortedTransformations )
            {
                IndexOverrideTransformation(
                    transformation,
                    transformationCollection,
                    auxiliaryMemberTransformations,
                    pendingInsertStatementContexts );

                this.IndexInjectTransformation(
                    input,
                    transformation,
                    diagnostics,
                    lexicalScopeFactory,
                    injectionNameProvider,
                    aspectReferenceSyntaxProvider,
                    transformationCollection,
                    replacedIntroduceDeclarationTransformations );

                IndexMemberLevelTransformation(
                    transformation,
                    transformationCollection,
                    input.FinalCompilationModel );

                this.IndexInsertStatementTransformation(
                    input,
                    diagnostics,
                    lexicalScopeFactory,
                    transformation,
                    transformationCollection,
                    auxiliaryMemberTransformations,
                    pendingInsertStatementContexts );

                IndexNodesWithModifiedAttributes( transformation, transformationCollection );
            }
        }

        IEnumerable<ISyntaxTreeTransformation> syntaxTreeTransformations;

        if ( !input.FinalCompilationModel.PartialCompilation.HasObservabilityFilter )
        {
            syntaxTreeTransformations = input.Transformations.OfType<ISyntaxTreeTransformation>();
        }
        else
        {
            // If there is observability filter, we need to always include all transformation of a partial type that has an observable part.
            syntaxTreeTransformations = GetObservableTransformationClosure( input );
        }

        // It's imperative that order of transformations is preserved while grouping by syntax tree.
        // The syntax tree we group by must be the main syntax tree of the enclosing type. We should never run transformations
        // of a partial type in parallel.
        var transformationsByCanonicalSyntaxTree = syntaxTreeTransformations.GroupBy( GetPrimarySyntaxTree );

        static SyntaxTree GetPrimarySyntaxTree( ISyntaxTreeTransformation syntaxTreeTransformation )
        {
            return GetCanonicalTargetDeclaration( syntaxTreeTransformation.TargetDeclaration ) switch
            {
                IFullRef<INamedType> namedType => namedType.PrimarySyntaxTree.AssertNotNull(),
                IFullRef<ICompilation> => syntaxTreeTransformation.TransformedSyntaxTree,
                var d => throw new AssertionFailedException( $"Unsupported: {d}" )
            };
        }

        static IFullRef<IDeclaration> GetCanonicalTargetDeclaration( IRef<IDeclaration> declaration )
        {
            return declaration switch
            {
                IFullRef<IMember> member => member.DeclaringType.AssertNotNull(),
                IFullRef<INamedType> type => type,
                IFullRef<IParameter> parameter => parameter.DeclaringType.AssertNotNull(),
                IFullRef<INamespace> @namespace => @namespace.Definition.Compilation.ToFullRef(),
                IFullRef<ICompilation> compilation => compilation,
                var d => throw new AssertionFailedException( $"Unsupported: {d}" )
            };
        }

        static IEnumerable<ISyntaxTreeTransformation> GetObservableTransformationClosure( AspectLinkerInput input )
        {
            var observedCanonicalTargetDeclarations = new HashSet<IRef<IDeclaration>>( RefEqualityComparer.Default );

            // Mark all transformed canonical declarations with an observable part as observable.
            foreach ( var transformation in input.Transformations.OfType<ISyntaxTreeTransformation>() )
            {
                var transformedSyntaxTree = transformation.TransformedSyntaxTree;

                if ( input.FinalCompilationModel.PartialCompilation.IsSyntaxTreeObserved( transformedSyntaxTree.FilePath ) )
                {
                    var canonicalTargetDeclaration = GetCanonicalTargetDeclaration( transformation.TargetDeclaration );
                    observedCanonicalTargetDeclarations.Add( canonicalTargetDeclaration );
                }
            }

            // Include all transformation with observable canonical target declaration.
            return input.Transformations.OfType<ISyntaxTreeTransformation>()
                .Where( t => observedCanonicalTargetDeclarations.Contains( GetCanonicalTargetDeclaration( t.TargetDeclaration ) ) );
        }

        await this._concurrentTaskRunner.RunConcurrentlyAsync( transformationsByCanonicalSyntaxTree, IndexTransformationsInSyntaxTree, cancellationToken );

        // Finalize non-auxiliary transformations (sorting).
        await transformationCollection.FinalizeAsync(
            this._concurrentTaskRunner,
            cancellationToken );

        void FlushPendingInsertStatementContext( KeyValuePair<IFullRef<IMember>, InsertStatementTransformationContextImpl> pair )
        {
            if ( RequiresAuxiliaryContractMember( pair.Key, pair.Value ) )
            {
                pair.Value.Complete();

                transformationCollection.AddTransformationCausingAuxiliaryOverride( pair.Value.OriginTransformation );

                // This may be the only "override" present, so make sure all other effects of overrides are present.
                AddSynthesizedSetterForPropertyIfRequired( pair.Key, transformationCollection );

                auxiliaryMemberTransformations
                    .GetOrAdd( pair.Key, _ => new AuxiliaryMemberTransformations() )
                    .InjectAuxiliaryContractMember(
                        pair.Value.OriginTransformation,
                        pair.Value.ReturnValueVariableName );
            }
        }

        await this._concurrentTaskRunner.RunConcurrentlyAsync( pendingInsertStatementContexts, FlushPendingInsertStatementContext, cancellationToken );

        // Process any auxiliary member transformations in parallel.
        void ProcessAuxiliaryMemberTransformations( KeyValuePair<IFullRef<IMember>, AuxiliaryMemberTransformations> transformationPair )
        {
            var member = transformationPair.Key;
            var transformations = transformationPair.Value;

            this.IndexAuxiliaryMemberTransformations(
                input.FinalCompilationModel,
                transformationCollection,
                lexicalScopeFactory,
                aspectReferenceSyntaxProvider,
                injectionNameProvider,
                member,
                transformations );
        }

        await this._concurrentTaskRunner.RunConcurrentlyAsync( auxiliaryMemberTransformations, ProcessAuxiliaryMemberTransformations, cancellationToken );

        var syntaxTreeForGlobalAttributes = input.FinalCompilationModel.PartialCompilation.SyntaxTreeForCompilationLevelAttributes;

        if ( !input.FinalCompilationModel.PartialCompilation.SyntaxTrees.ContainsKey( syntaxTreeForGlobalAttributes.FilePath )
             && input.Transformations.OfType<IntroduceAttributeTransformation>().Any( t => t.TransformedSyntaxTree == syntaxTreeForGlobalAttributes ) )
        {
            transformationCollection.AddIntroducedSyntaxTree( syntaxTreeForGlobalAttributes );
        }

        // Replace wildcard AssemblyVersionAttribute with actual version.
        var attributes = input.FinalCompilationModel.GetAttributeCollection( input.FinalCompilationModel.ToFullRef() );
        var assemblyVersionAttributeType = (INamedType) input.FinalCompilationModel.Factory.GetTypeByReflectionType( typeof(AssemblyVersionAttribute) );
        var assemblyVersionAttribute = input.FinalCompilationModel.Attributes.OfAttributeType( assemblyVersionAttributeType ).FirstOrDefault();

#pragma warning disable CA1307 // Specify StringComparison for clarity
        if ( assemblyVersionAttribute?.ConstructorArguments.FirstOrDefault() is { Value: string version }
             && version.Contains( '*' ) )
        {
            attributes.Remove( assemblyVersionAttributeType.ToFullRef() );

            // It's hacky to add an AttributeBuilder with null Advice, but it seems to work fine.
            // We avoid to use user APIs that require a user code execution context.
            var assemblyVersionAttributeConstructor =
                assemblyVersionAttributeType.Constructors.Single( x => x.Parameters is [{ Type.SpecialType: SpecialType.String }] );

            var newAssemblyVersionAttribute =
                new StandaloneAttributeData( assemblyVersionAttributeConstructor )
                {
                    ConstructorArguments = ImmutableArray.Create(
                        TypedConstant.Create(
                            input.FinalCompilationModel.RoslynCompilation.Assembly.Identity.Version.ToString(),
                            assemblyVersionAttributeConstructor.Parameters[0].Type ) )
                };

            var attributeBuilder = new AttributeBuilder(
                null!,
                input.FinalCompilationModel.DeclaringAssembly,
                newAssemblyVersionAttribute );

            attributeBuilder.Freeze();

            attributes.Add( attributeBuilder.BuilderData );
        }
#pragma warning restore CA1307

        // Add syntax trees that were introduced (typically empty). These are trees currently created by transformation and the 
        // intermediate registry needs to create a map of transformation target syntax tree to modified syntax tree.
        var compilationWithIntroducedTrees =
            input.FinalCompilationModel.PartialCompilation.Update(
                transformationCollection.IntroducedSyntaxTrees.SelectAsArray( SyntaxTreeTransformation.AddTree ) );

        // Update the syntax trees and create a new partial compilation.
        var transformations = new ConcurrentQueue<SyntaxTreeTransformation>();

        async Task RewriteSyntaxTreeAsync( SyntaxTree initialSyntaxTree )
        {
            Rewriter rewriter = new(
                this,
                transformationCollection,
                input.FinalCompilationModel,
                syntaxTreeForGlobalAttributes );

            var oldRoot = await initialSyntaxTree.GetRootAsync( cancellationToken );
            var newRoot = rewriter.Visit( oldRoot ).AssertNotNull();

            if ( oldRoot != newRoot )
            {
                var intermediateSyntaxTree = initialSyntaxTree.WithRootAndOptions( newRoot, initialSyntaxTree.Options );

                transformations.Enqueue( SyntaxTreeTransformation.ReplaceTree( initialSyntaxTree, intermediateSyntaxTree ) );
            }
        }

        await
            this._concurrentTaskRunner.RunConcurrentlyAsync(
                compilationWithIntroducedTrees.SyntaxTrees.Values,
                RewriteSyntaxTreeAsync,
                cancellationToken );

        var helperSyntaxTree = injectionHelperProvider.GetLinkerHelperSyntaxTree( compilationWithIntroducedTrees.LanguageOptions );
        transformations.Enqueue( SyntaxTreeTransformation.AddTree( helperSyntaxTree ) );

        var intermediateCompilation = compilationWithIntroducedTrees.Update( transformations );

        // Report the linker intermediate compilation to tooling/tests.
        this._serviceProvider.GetService<ILinkerObserver>()
            ?.OnIntermediateCompilationCreated( intermediateCompilation );

        var injectionRegistry = new LinkerInjectionRegistry(
            transformationComparer,
            intermediateCompilation,
            transformations,
            transformationCollection.InjectedMembers,
            transformationCollection.BuilderToTransformationMap,
            transformationCollection.IntroducedParametersByTargetDeclaration,
            transformationCollection.TransformationsCausingAuxiliaryOverrides,
            this._concurrentTaskRunner,
            cancellationToken );

        var lateTransformationRegistry =
            new LinkerLateTransformationRegistry(
                intermediateCompilation,
                transformationCollection.LateTypeLevelTransformations );

        var projectOptions = this._serviceProvider.GetService<IProjectOptions>();

        return
            new LinkerInjectionStepOutput(
                diagnostics,
                input.InitialCompilationModel,
                input.FinalCompilationModel,
                intermediateCompilation,
                injectionRegistry,
                lateTransformationRegistry,
                input.OrderedAspectLayers,
                projectOptions );
    }

    private static void IndexNodesWithModifiedAttributes(
        ISyntaxTreeTransformation transformation,
        TransformationCollection transformationCollection )
    {
        // We only need to index transformations on syntax (i.e. on source code) because introductions on generated code
        // are taken from the compilation model.

        // Note: Compilation-level attributes will not be indexed because the containing declaration has no
        // syntax reference.

        switch ( transformation )
        {
            case IntroduceAttributeTransformation { TargetDeclaration: ISymbolRef symbolRef }:
                {
                    foreach ( var declaringSyntax in symbolRef.Symbol.DeclaringSyntaxReferences )
                    {
                        transformationCollection.AddNodeWithModifiedAttributes( declaringSyntax.GetSyntax() );
                    }

                    break;
                }

            case RemoveAttributesTransformation { TargetDeclaration: ISymbolRef symbolRef }:
                {
                    foreach ( var declaringSyntax in symbolRef.Symbol.DeclaringSyntaxReferences )
                    {
                        transformationCollection.AddNodeWithModifiedAttributes( declaringSyntax.GetSyntax() );
                    }

                    break;
                }
        }
    }

    private static void IndexIntroduceDeclarationTransformation(
        ImmutableDictionary<string, SyntaxTree> existingSyntaxTrees,
        ISyntaxTreeTransformation transformation,
        TransformationCollection transformationCollection )
    {
        if ( transformation is IIntroduceDeclarationTransformation introduceDeclarationTransformation )
        {
            transformationCollection.AddIntroduceTransformation(
                introduceDeclarationTransformation.DeclarationBuilderData,
                introduceDeclarationTransformation );

            if ( !existingSyntaxTrees.ContainsKey( transformation.TransformedSyntaxTree.FilePath ) )
            {
                transformationCollection.AddIntroducedSyntaxTree( transformation.TransformedSyntaxTree );
            }
        }
    }

    private static void IndexReplaceTransformation(
        ITransformation transformation,
        TransformationCollection transformationCollection,
        HashSet<IIntroduceDeclarationTransformation> replacedIntroduceDeclarationTransformations )
    {
        if ( transformation is not IReplaceMemberTransformation replaceMemberTransformation || replaceMemberTransformation.ReplacedMember == null )
        {
            return;
        }

        // We want to get the replaced member as it is in the compilation of the transformation, i.e. with applied redirections up to that point.
        // TODO: the target may have been removed from the
        var replacedDeclaration = replaceMemberTransformation.ReplacedMember;

        switch ( replacedDeclaration )
        {
            case ISymbolRef<IField> sourceField:
                var fieldSyntaxReference =
                    sourceField.Symbol.GetPrimarySyntaxReference()
                    ?? throw new AssertionFailedException( $"The field '{sourceField}' does not have syntax." );

                var removedFieldSyntax = fieldSyntaxReference.GetSyntax();
                transformationCollection.AddRemovedSyntax( removedFieldSyntax );

                break;

            case ISymbolRef<IConstructor> replacedConstructor:
                Invariant.Assert( replacedConstructor.Symbol.GetPrimarySyntaxReference() == null );

                break;

            // This needs to point to an interface
            case IIntroducedRef replacedBuilder:
                if ( !transformationCollection.TryGetIntroduceDeclarationTransformation(
                        replacedBuilder.BuilderData,
                        out var introduceDeclarationTransformation ) )
                {
                    throw new AssertionFailedException( $"Builder {replacedBuilder} is missing registered transformation." );
                }

                lock ( replacedIntroduceDeclarationTransformations )
                {
                    replacedIntroduceDeclarationTransformations.Add( introduceDeclarationTransformation );
                }

                break;

            default:
                throw new AssertionFailedException( $"Unexpected replace declaration: '{replacedDeclaration}'." );
        }
    }

    private void IndexInjectTransformation(
        AspectLinkerInput input,
        ITransformation transformation,
        UserDiagnosticSink diagnostics,
        LexicalScopeFactory lexicalScopeFactory,
        LinkerInjectionNameProvider nameProvider,
        LinkerAspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        TransformationCollection transformationCollection,
        HashSet<IIntroduceDeclarationTransformation> replacedIntroduceDeclarationTransformations )
    {
        if ( transformation is IIntroduceDeclarationTransformation introduceDeclarationTransformation )
        {
            lock ( replacedIntroduceDeclarationTransformations )
            {
                if ( replacedIntroduceDeclarationTransformations.Contains( introduceDeclarationTransformation ) )
                {
                    return;
                }
            }
        }

        switch ( transformation )
        {
            case IInjectMemberTransformation injectMemberTransformation:
                {
                    // Transformed syntax tree must match insert position.
                    Invariant.Assert( injectMemberTransformation.TransformedSyntaxTree == injectMemberTransformation.InsertPosition.SyntaxTree );

                    // Create the SyntaxGenerationContext for the insertion point.
                    var syntaxGenerationContext = this._compilationContext.GetSyntaxGenerationContext(
                        this._syntaxGenerationOptions,
                        injectMemberTransformation.InsertPosition );

                    // TODO: It smells that we pass original compilation here. Should be the compilation for the transformation.
                    //       For introduction, this should be a compilation that INCLUDES the builder.
                    //       But, if we pass the mutable compilation, it will get changed before the template is expanded.
                    //       The expanded template should not see declarations added after it runs.

                    // Call GetInjectedMembers.
                    var injectionContext = new MemberInjectionContext(
                        this._serviceProvider,
                        diagnostics,
                        nameProvider,
                        aspectReferenceSyntaxProvider,
                        lexicalScopeFactory,
                        syntaxGenerationContext,
                        input.FinalCompilationModel );

                    var injectedMembers = injectMemberTransformation.GetInjectedMembers( injectionContext );

                    transformationCollection.AddInjectedMembers( injectMemberTransformation, injectedMembers );

                    break;
                }

            case IInjectInterfaceTransformation injectInterfaceTransformation:
                {
                    // Create the SyntaxGenerationContext for the insertion point.
                    var syntaxGenerationContext = this._compilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions );

                    var introducedInterfaceSyntax = injectInterfaceTransformation.GetSyntax( syntaxGenerationContext );
                    var introducedInterface = new LinkerInjectedInterface( injectInterfaceTransformation, introducedInterfaceSyntax );

                    transformationCollection.AddInjectedInterface( injectInterfaceTransformation.TargetDeclaration.As<INamedType>(), introducedInterface );

                    break;
                }
        }
    }

    private static void IndexOverrideTransformation(
        ITransformation transformation,
        TransformationCollection transformationCollection,
        ConcurrentDictionary<IFullRef<IMember>, AuxiliaryMemberTransformations> auxiliaryMemberTransformations,
        ConcurrentDictionary<IFullRef<IMember>, InsertStatementTransformationContextImpl> pendingInsertStatementContexts )
    {
        if ( transformation is not IOverrideDeclarationTransformation overrideDeclarationTransformation )
        {
            return;
        }

        AddSynthesizedSetterForPropertyIfRequired(
            overrideDeclarationTransformation.OverriddenDeclaration,
            transformationCollection );

        if ( overrideDeclarationTransformation.OverriddenDeclaration is IFullRef<IConstructor> { Definition.IsPrimary: true } overriddenConstructorRef )
        {
            auxiliaryMemberTransformations.GetOrAdd( overriddenConstructorRef, _ => new AuxiliaryMemberTransformations() ).InjectAuxiliarySourceMember();

            transformationCollection
                .GetOrAddLateTypeLevelTransformations( (ISymbolRef<INamedType>) overriddenConstructorRef.ContainingDeclaration.AssertNotNull() )
                .RemovePrimaryConstructor();
        }

        if ( overrideDeclarationTransformation.OverriddenDeclaration is IFullRef<IMember> overriddenMember
             && pendingInsertStatementContexts.TryGetValue( overriddenMember, out var insertStatementContext ) )
        {
            // Remove context for the insert statement (there should be no race since we parallelize based on containing type).
            pendingInsertStatementContexts.TryRemove( overriddenMember, out _ );

            // Auxiliary member is needed for output contracts
            if ( RequiresAuxiliaryContractMember( overriddenMember, insertStatementContext ) )
            {
                insertStatementContext.Complete();

                transformationCollection.AddTransformationCausingAuxiliaryOverride( insertStatementContext.OriginTransformation );

                auxiliaryMemberTransformations
                    .GetOrAdd( overriddenMember, _ => new AuxiliaryMemberTransformations() )
                    .InjectAuxiliaryContractMember(
                        insertStatementContext.OriginTransformation,
                        insertStatementContext.ReturnValueVariableName );
            }
        }
    }

    private static void AddSynthesizedSetterForPropertyIfRequired(
        IFullRef<IDeclaration> overriddenDeclarationRef,
        TransformationCollection transformationCollection )
    {
        var overriddenDeclaration = overriddenDeclarationRef.Definition;

        // If this is an auto-property that does not override a base property, we can add synthesized init-only setter.
        // If this is overridden property we need to:
        //  1) Block inlining of the first override (force the trampoline).
        //  2) Substitute all sets of the property (can be only in constructors) to use the first override instead.
        if ( overriddenDeclaration is IProperty
            {
                IsAutoPropertyOrField: true, Writeability: Writeability.ConstructorOnly, SetMethod.IsImplicitlyDeclared: true,
                OverriddenProperty: null or { SetMethod: not null }
            } overriddenAutoProperty )
        {
            transformationCollection.AddAutoPropertyWithSynthesizedSetter( overriddenAutoProperty.ToRef() );
        }
    }

    private void IndexInsertStatementTransformation(
        AspectLinkerInput input,
        UserDiagnosticSink diagnostics,
        LexicalScopeFactory lexicalScopeFactory,
        ITransformation transformation,
        TransformationCollection transformationCollection,
        ConcurrentDictionary<IFullRef<IMember>, AuxiliaryMemberTransformations> auxiliaryMemberTransformations,
        ConcurrentDictionary<IFullRef<IMember>, InsertStatementTransformationContextImpl> pendingInsertStatementContexts )
    {
        if ( transformation is not IInsertStatementTransformation insertStatementTransformation )
        {
            return;
        }

        var targetMember = insertStatementTransformation.TargetMember.Definition;

        var syntaxGenerationContext
            = this._compilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions, targetMember );

        switch ( targetMember )
        {
            case IPropertyOrIndexer propertyOrIndexer:
                {
                    var insertedStatements = GetInsertedStatements();

                    // Note that we are doing cross-compilation comparisons here below.

                    if ( propertyOrIndexer.GetMethod != null )
                    {
                        transformationCollection.AddInsertedStatements(
                            propertyOrIndexer.GetMethod.ToRef(),
                            insertedStatements
                                .Where(
                                    s =>
                                        s.ContextDeclaration.IsContainedIn( propertyOrIndexer.GetMethod )
                                        || (propertyOrIndexer is IIndexer indexer && s.ContextDeclaration is IParameter parameter
                                                                                  && parameter.ContainingDeclaration!.Equals( indexer )) )
                                .ToReadOnlyList() );
                    }

                    if ( propertyOrIndexer.SetMethod != null )
                    {
                        transformationCollection.AddInsertedStatements(
                            propertyOrIndexer.SetMethod.ToRef(),
                            insertedStatements
                                .Where(
                                    s =>
                                        s.ContextDeclaration.IsContainedIn( propertyOrIndexer.SetMethod )
                                        || (propertyOrIndexer is IIndexer indexer && s.ContextDeclaration is IParameter parameter
                                                                                  && parameter.ContainingDeclaration!.Equals( indexer )) )
                                .ToReadOnlyList() );
                    }

                    break;
                }

            case IMethodBase methodBase:
                {
                    var insertedStatements = GetInsertedStatements();

                    transformationCollection.AddInsertedStatements( methodBase.ToRef(), insertedStatements );

                    break;
                }

            default:
                throw new AssertionFailedException( $"Unexpected target: {targetMember}." );
        }

        if ( targetMember is IConstructor { IsPrimary: true } overriddenConstructor )
        {
            auxiliaryMemberTransformations.GetOrAdd( overriddenConstructor.ToFullRef(), _ => new AuxiliaryMemberTransformations() )
                .InjectAuxiliarySourceMember();

            transformationCollection.GetOrAddLateTypeLevelTransformations( (ISymbolRef<INamedType>) overriddenConstructor.DeclaringType.ToRef() )
                .RemovePrimaryConstructor();
        }

        IReadOnlyList<InsertedStatement> GetInsertedStatements()
        {
            // Contexts for inserting statements are reused until the next override of the target declaration.
            var context =
                pendingInsertStatementContexts.GetOrAdd(
                    insertStatementTransformation.TargetMember,
                    m => new InsertStatementTransformationContextImpl(
                        this._serviceProvider,
                        diagnostics,
                        syntaxGenerationContext,
                        input.FinalCompilationModel,
                        lexicalScopeFactory,
                        insertStatementTransformation,
                        m ) );

            var statements = insertStatementTransformation.GetInsertedStatements( context );

            var markedForInputContracts = false;
            var markedForOutputContracts = false;

            foreach ( var statement in statements )
            {
                if ( !markedForInputContracts && statement.Kind == InsertedStatementKind.InputContract )
                {
                    markedForInputContracts = true;
                    context.MarkAsUsedForInputContracts();
                }

                if ( !markedForOutputContracts && statement.Kind == InsertedStatementKind.OutputContract )
                {
                    markedForOutputContracts = true;
                    context.MarkAsUsedForOutputContracts();

                    if ( targetMember is IProperty or IIndexer
                         || (targetMember is IMethod method && method.GetAsyncInfo().ResultType.SpecialType != SpecialType.Void) )
                    {
                        // Force the return variable name to be allocated if the return type is not void.
                        // If there are output contracts that don't use the return value, the return value is still required.
                        // If not done here, the return value would be allocated later, leading to unintuitive variable name ordinals.
                        _ = context.GetReturnValueVariableName();
                    }
                }

                if ( markedForInputContracts && markedForOutputContracts )
                {
                    break;
                }
            }

#if DEBUG
            foreach ( var statement in statements )
            {
                if ( statement.Statement is BlockSyntax block )
                {
                    if ( !block.Statements.All( s => s.HasAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ) ) )
                    {
                        throw new AssertionFailedException( "GeneratedCodeAnnotationKind annotation missing." );
                    }
                }
                else
                {
                    if ( !statement.Statement.HasAnnotations( FormattingAnnotations.GeneratedCodeAnnotationKind ) )
                    {
                        throw new AssertionFailedException( "GeneratedCodeAnnotationKind annotation missing." );
                    }
                }
            }
#endif

            return statements;
        }
    }

    private static void IndexMemberLevelTransformation(
        ITransformation transformation,
        TransformationCollection transformationCollection,
        CompilationModel compilationModel )
    {
        if ( transformation is not IMemberLevelTransformation memberLevelTransformation )
        {
            return;
        }

        // TODO: Supports only constructors without overrides.
        //       Needs to be generalized for anything else (take into account overrides).

        var memberLevelTransformations =
            transformationCollection.GetOrAddMemberLevelTransformations( memberLevelTransformation.TargetMember );

        switch ( transformation )
        {
            case IntroduceParameterTransformation introduceParameterTransformation:

                if ( introduceParameterTransformation.TargetDeclaration is IIntroducedRef ||
                     compilationModel.IsRedirected( introduceParameterTransformation.TargetDeclaration ) )
                {
                    // Parameters introduced into introduced constructors are discovered by IntroduceParameterTransforma because they
                    // are a part of the CompilationModel.
                }
                else
                {
                    memberLevelTransformations.Add( introduceParameterTransformation );
                    transformationCollection.AddIntroducedParameter( introduceParameterTransformation );
                }

                break;

            case IntroduceConstructorInitializerArgumentTransformation appendArgumentTransformation:
                memberLevelTransformations.Add( appendArgumentTransformation );

                break;

            default:
                throw new AssertionFailedException( $"Unexpected combination: ('{transformation}', '{memberLevelTransformation.TargetMember}')." );
        }
    }

    private void IndexAuxiliaryMemberTransformations(
        CompilationModel finalCompilationModel,
        TransformationCollection transformationCollection,
        LexicalScopeFactory lexicalScopeFactory,
        AspectReferenceSyntaxProvider aspectReferenceSyntaxProvider,
        LinkerInjectionNameProvider injectionNameProvider,
        IFullRef<IMember> member,
        AuxiliaryMemberTransformations transformations )
    {
        var auxiliaryMemberFactory =
            new AuxiliaryMemberFactory(
                this,
                finalCompilationModel,
                lexicalScopeFactory,
                aspectReferenceSyntaxProvider,
                injectionNameProvider,
                transformationCollection );

        if ( transformations.ShouldInjectAuxiliarySourceMember )
        {
            switch ( member )
            {
                case IFullRef<IConstructor> { Definition.IsPrimary: true } primaryConstructor:
                    transformationCollection.AddInjectedMember(
                        new InjectedMember(
                            null,
                            DeclarationKind.Constructor,
                            auxiliaryMemberFactory.GetAuxiliarySourceConstructor( primaryConstructor ),
                            null,
                            InjectedMemberSemantic.AuxiliaryBody,
                            primaryConstructor ) );

                    break;

                default:
                    throw new AssertionFailedException( $"Unsupported: {member}" );
            }
        }

        foreach ( var (originTransformation, returnVariableName) in transformations.AuxiliaryContractMembers )
        {
            // Having a record in this list means that there is an output contract, which requires a trivial body that will act as a receiver of contracts.
            // Usually this means that there is an output contract, otherwise all input contracts are injected into the preceding override or source.
            // Example:
            // <input_contracts> 
            // var returnValue = meta.Proceed();
            // <output_contracts>
            // return returnValue;

            var advice = originTransformation.AspectLayerInstance;
            var rootMember = member.GetTypeMember();

            // TODO: Ideally, entry + exit statements should be injected here, but it complicates the transformation collection and rewriter.
            //       This now generates "well-known" structure, which is recognized by the rewriter, which is quite ugly.
            //       TransformationCollection is not finalized at this point and now selects statements based on InjectedMember, which we are creating here.

            transformationCollection.AddInjectedMember(
                new InjectedMember(
                    originTransformation,
                    member.DeclarationKind,
                    auxiliaryMemberFactory.GetAuxiliaryContractMember( rootMember, advice, returnVariableName ),
                    advice.AspectLayerId,
                    InjectedMemberSemantic.AuxiliaryBody,
                    rootMember ) );
        }
    }

    // TODO: This is not optimal for cases with no output contracts, because we need this only to have "an override" to force other transformations.
    //       But for these declarations, the auxiliary member is created always, even when there are no input contracts.
    private static bool RequiresAuxiliaryContractMember(
        IFullRef<IMember> member,
        InsertStatementTransformationContextImpl insertStatementContext )
        => insertStatementContext.WasUsedForOutputContracts
           || member is IFullRef<IFieldOrProperty> { Definition.IsAutoPropertyOrField: true } || (member is IFullRef<IMethod>
           {
               Definition: { ContainingDeclaration: IFieldOrProperty { IsAutoPropertyOrField: true } } or
               { IsPartial: true, HasImplementation: false }
           } && insertStatementContext.WasUsedForInputContracts);
}