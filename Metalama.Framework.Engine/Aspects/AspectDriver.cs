// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Eligibility.Implementation;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Configuration;
using Metalama.Framework.Engine.DesignTime.CodeFixes;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Licensing;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Options;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Executes aspects.
/// </summary>
internal sealed class AspectDriver : IAspectDriver
{
    private readonly IAspectClassImpl _aspectClass;
    private readonly CodeFixAvailability _codeFixAvailability;

    public IEligibilityRule<IDeclaration>? EligibilityRule { get; }

    public AspectDriver( ProjectServiceProvider serviceProvider, IAspectClassImpl aspectClass, CompilationModel compilation )
    {
        this._aspectClass = aspectClass;

        // We don't store the GlobalServiceProvider because the AspectDriver is created during the pipeline initialization but used
        // during pipeline execution, and execution has a different service provider.

        // Introductions must have a deterministic order because of testing.
        // We can pass a null TemplateProvider here because the templates will not be executed, but only used to discover eligibility.
        var declarativeAdviceAttributes = aspectClass
            .TemplateClasses.SelectMany( c => c.GetDeclarativeAdvice( serviceProvider, compilation, default, ObjectReader.Empty ) )
            .ToReadOnlyList();

        if ( declarativeAdviceAttributes.Count > 0 )
        {
            foreach ( var declarativeAdvice in declarativeAdviceAttributes )
            {
                var eligibilityBuilder = new EligibilityBuilder<IDeclaration>();

                ((DeclarativeAdviceAttribute) declarativeAdvice.AdviceAttribute!).BuildAspectEligibility(
                    eligibilityBuilder,
                    declarativeAdvice.GetDeclaration( compilation ) );

                this.EligibilityRule = eligibilityBuilder.Build();
            }
        }

        // Determine the licensing abilities of the current aspect class.
        var licenseVerifier = serviceProvider.GetService<LicenseVerifier>();

        if ( licenseVerifier == null || licenseVerifier.VerifyCanApplyCodeFix( aspectClass ) )
        {
            this._codeFixAvailability = CodeFixAvailability.PreviewAndApply;
        }
        else
        {
            var designTimeConfiguration = serviceProvider.Global.GetRequiredBackstageService<IConfigurationManager>().Get<DesignTimeConfiguration>();
            this._codeFixAvailability = designTimeConfiguration.HideUnlicensedCodeActions ? CodeFixAvailability.None : CodeFixAvailability.PreviewOnly;
        }
    }

    private static void ApplyOptions(
        IAspectInstanceInternal aspectInstance,
        IDeclaration declaration,
        IUserDiagnosticSink diagnosticSink,
        UserCodeInvoker invoker,
        UserCodeExecutionContext baseContext )
    {
        if ( aspectInstance.Aspect is IHierarchicalOptionsProvider optionsProvider )
        {
            var invokerContext = baseContext.WithDescription( UserCodeDescription.Create( "executing GetOptions() for {0}", aspectInstance ) );

            var providerContext = new OptionsProviderContext(
                declaration,
                new ScopedDiagnosticSink( diagnosticSink, aspectInstance, declaration, declaration ) );

            var optionList = invoker.Invoke( () => optionsProvider.GetOptions( providerContext ).ToReadOnlyList(), invokerContext );

            foreach ( var options in optionList )
            {
                declaration.GetCompilationModel().HierarchicalOptionsManager.AssertNotNull().SetAspectOptions( declaration, options );
            }
        }
    }

    public Task<AspectInstanceResult> ExecuteAspectAsync(
        IAspectInstanceInternal aspectInstance,
        string? layer,
        CompilationModel initialCompilationRevision,
        CompilationModel currentCompilationRevision,
        AspectPipelineConfiguration pipelineConfiguration,
        int pipelineStepIndex,
        int indexWithinType,
        CancellationToken cancellationToken )
    {
        var target = aspectInstance.TargetDeclaration.GetTarget( initialCompilationRevision );

        return target switch
        {
            ICompilation compilation => EvaluateAspectImpl( compilation ),
            INamedType type => EvaluateAspectImpl( type ),
            IMethod method => EvaluateAspectImpl( method ),
            IField field => EvaluateAspectImpl( field ),
            IProperty property => EvaluateAspectImpl( property ),
            IIndexer indexer => EvaluateAspectImpl( indexer ),
            IConstructor constructor => EvaluateAspectImpl( constructor ),
            IParameter parameter => EvaluateAspectImpl( parameter ),
            ITypeParameter genericParameter => EvaluateAspectImpl( genericParameter ),
            IEvent @event => EvaluateAspectImpl( @event ),
            INamespace ns => EvaluateAspectImpl( ns ),
            _ => throw new NotSupportedException( $"Cannot add an aspect to a declaration of type {target.DeclarationKind}." )
        };

        async Task<AspectInstanceResult> EvaluateAspectImpl<T>( T targetDeclaration )
            where T : class, IDeclaration
        {
            if ( aspectInstance.IsSkipped )
            {
                // The aspect instance was skipped from a previous layer.
                return new AspectInstanceResult(
                    aspectInstance,
                    AdviceOutcome.Ignore,
                    ImmutableUserDiagnosticList.Empty,
                    ImmutableArray<ITransformation>.Empty,
                    ImmutableArray<IAspectSource>.Empty,
                    ImmutableArray<IValidatorSource>.Empty,
                    ImmutableArray<IHierarchicalOptionsSource>.Empty );
            }

            AspectInstanceResult CreateResultForError( Diagnostic diagnostic )
            {
                return new AspectInstanceResult(
                    aspectInstance,
                    AdviceOutcome.Error,
                    new ImmutableUserDiagnosticList( ImmutableArray.Create( diagnostic ), ImmutableArray<ScopedSuppression>.Empty ),
                    ImmutableArray<ITransformation>.Empty,
                    ImmutableArray<IAspectSource>.Empty,
                    ImmutableArray<IValidatorSource>.Empty,
                    ImmutableArray<IHierarchicalOptionsSource>.Empty );
            }

            cancellationToken.ThrowIfCancellationRequested();

            var serviceProvider = pipelineConfiguration.ServiceProvider;

            // Map the target declaration to the correct revision of the compilation model.
            targetDeclaration = initialCompilationRevision.Factory.Translate( targetDeclaration ).AssertNotNull();

            if ( aspectInstance.Aspect is not IAspect<T> aspectOfT )
            {
                // TODO: should the diagnostic be applied to the attribute, if one exists?

                // Get the code model type for the reflection type so we have better formatting of the diagnostic.
                var interfaceType = initialCompilationRevision.CompilationContext.ReflectionMapper.GetTypeSymbol( typeof(IAspect<T>) ).AssertNotNull();

                var diagnostic =
                    GeneralDiagnosticDescriptors.AspectAppliedToIncorrectDeclaration.CreateRoslynDiagnostic(
                        targetDeclaration.GetDiagnosticLocation(),
                        (AspectType: this._aspectClass.ShortName, targetDeclaration.DeclarationKind, targetDeclaration, interfaceType),
                        aspectInstance );

                return CreateResultForError( diagnostic );
            }

            var diagnosticSink = new UserDiagnosticSink(
                this._aspectClass.Project,
                pipelineConfiguration.CodeFixFilter,
                this._codeFixAvailability );

            // This is used for compilation aspects.
            // Knowing that the aspect is applied to a compilation is not particularly useful when we want to understand dependencies between source files.
            var predecessorTrees = aspectInstance.PredecessorTreeClosure;

            var buildAspectExecutionContext = new UserCodeExecutionContext(
                serviceProvider,
                UserCodeDescription.Create( "executing BuildAspect for {0}", aspectInstance ),
                initialCompilationRevision.CompilationContext,
                new AspectLayerId( this._aspectClass ),
                initialCompilationRevision,
                targetDeclaration,
                sourceTrees: predecessorTrees,
                diagnostics: diagnosticSink,
                throwOnUnsupportedDependencies: true );

            var userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();

            // Apply options.
            ApplyOptions( aspectInstance, targetDeclaration, diagnosticSink, userCodeInvoker, buildAspectExecutionContext );

            // Create the AspectLayerInstance.
            var aspectLayerInstance = new AspectLayerInstance( aspectInstance, layer, initialCompilationRevision );

            // Create the AdviceFactory.
            var adviceFactoryState = new AdviceFactoryState(
                serviceProvider,
                aspectLayerInstance,
                currentCompilationRevision,
                diagnosticSink,
                buildAspectExecutionContext,
                pipelineStepIndex,
                indexWithinType );

            var adviceFactory = new AdviceFactory<T>(
                targetDeclaration,
                adviceFactoryState,
                aspectInstance.TemplateInstances.Count == 1 ? aspectInstance.TemplateInstances.Values.Single() : null,
                layer,
                null,
                diagnosticSink );

            // Create the AspectBuilder.
            var aspectBuilderState = new AspectBuilderState(
                serviceProvider,
                diagnosticSink,
                pipelineConfiguration,
                aspectInstance,
                adviceFactoryState,
                layer,
                cancellationToken );

            var aspectBuilder = new AspectBuilder<T>( targetDeclaration, aspectBuilderState, adviceFactory );

            adviceFactoryState.AspectBuilderState = aspectBuilderState;

            // Prepare declarative advice.
            var declarativeAdvice = this._aspectClass.TemplateClasses
                .SelectMany(
                    c => c.GetDeclarativeAdvice(
                        serviceProvider,
                        initialCompilationRevision,
                        TemplateProvider.FromInstance( aspectInstance.Aspect ),
                        adviceFactoryState.AspectBuilderState.GetTagsReader( null ) ) )
                .ToReadOnlyList();

            if ( !userCodeInvoker
                    .TryInvoke(
                        () =>
                        {
                            // Execute declarative advice.
                            foreach ( var advice in declarativeAdvice )
                            {
                                ((DeclarativeAdviceAttribute) advice.AdviceAttribute.AssertNotNull()).BuildAdvice(
                                    advice.GetDeclaration( initialCompilationRevision ),
                                    advice.TemplateClassMember.Key,
                                    aspectBuilder );
                            }

                            if ( !aspectBuilder.IsAspectSkipped )
                            {
                                aspectOfT.BuildAspect( aspectBuilder );
                            }
                        },
                        buildAspectExecutionContext ) )
            {
                aspectInstance.Skip();

                return
                    new AspectInstanceResult(
                        aspectInstance,
                        AdviceOutcome.Error,
                        diagnosticSink.ToImmutable(),
                        ImmutableArray<ITransformation>.Empty,
                        ImmutableArray<IAspectSource>.Empty,
                        ImmutableArray<IValidatorSource>.Empty,
                        ImmutableArray<IHierarchicalOptionsSource>.Empty );
            }

            var aspectResult = aspectBuilderState.ToResult();

            if ( aspectResult.Outcome == AdviceOutcome.Error )
            {
                aspectInstance.Skip();
            }
            else if ( aspectResult.Outcome != AdviceOutcome.Ignore )
            {
                // Validators on the current version of the compilation must be executed now.

                if ( !aspectResult.ValidatorSources.IsDefaultOrEmpty )
                {
                    diagnosticSink.Reset();

                    var validationRunner = new ValidationRunner( pipelineConfiguration, aspectResult.ValidatorSources );

                    await validationRunner.RunDeclarationValidatorsAsync(
                        initialCompilationRevision,
                        CompilationModelVersion.Current,
                        diagnosticSink,
                        cancellationToken );

                    if ( !diagnosticSink.IsEmpty )
                    {
                        aspectResult = aspectResult.WithAdditionalDiagnostics( diagnosticSink.ToImmutable() );
                    }
                }
            }

            return aspectResult;
        }
    }
}