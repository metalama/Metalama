// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Licensing;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Project;
using Metalama.Framework.Validation;
using System;
using System.Reflection;
using System.Threading;

namespace Metalama.Framework.Engine.Aspects
{
    internal sealed class AspectBuilder<T> : IAspectBuilder<T>, IAspectBuilderInternal, IAspectReceiverParent
        where T : class, IDeclaration
    {
        private readonly AspectBuilderState _aspectBuilderState;
        private readonly AdviceFactory<T> _adviceFactory;

        public AspectBuilder(
            T target,
            AspectBuilderState aspectBuilderState,
            AdviceFactory<T> adviceFactory,
            AspectPredecessor? aspectPredecessor = null )
        {
            this.Target = target;
            this._aspectBuilderState = aspectBuilderState;
            this._adviceFactory = adviceFactory;
            this.AspectPredecessor = aspectPredecessor ?? new AspectPredecessor( AspectPredecessorKind.ChildAspect, aspectBuilderState.AspectInstance );
            this.LicenseVerifier = this.ServiceProvider.GetService<LicenseVerifier>();
        }

        public IProject Project => this.Target.Compilation.Project;

        [Memo]
        public string? Namespace => this.Target.GetNamespace()?.FullName;

        public IAspectInstance AspectInstance => this._aspectBuilderState.AspectInstance;

        void IPipelineContributorSourceCollector.AddAspectSource( IAspectSource aspectSource )
        {
            this._aspectBuilderState.AspectSources = this._aspectBuilderState.AspectSources.Add( aspectSource );
        }

        void IPipelineContributorSourceCollector.AddValidatorSource( IValidatorSource validatorSource )
        {
            this._aspectBuilderState.ValidatorSources = this._aspectBuilderState.ValidatorSources.Add( validatorSource );
        }

        public void AddOptionsSource( IHierarchicalOptionsSource hierarchicalOptionsSource )
        {
            this._aspectBuilderState.OptionsSources = this._aspectBuilderState.OptionsSources.Add( hierarchicalOptionsSource );
        }

        public ProjectServiceProvider ServiceProvider => this._aspectBuilderState.ServiceProvider;

        [Obsolete]
        IAdviceFactory IAspectBuilder.Advice => this._adviceFactory;

        IAdviceFactory IAdviserInternal.AdviceFactory => this._adviceFactory;

        public DisposeAction WithPredecessor( in AspectPredecessor predecessor )
        {
            var oldPredecessor = this.AspectPredecessor;
            this.AspectPredecessor = predecessor;

            return new DisposeAction( () => this.AspectPredecessor = oldPredecessor );
        }

        IDiagnosticAdder IAspectBuilderInternal.DiagnosticAdder => this._aspectBuilderState.Diagnostics;

        public ScopedDiagnosticSink Diagnostics => this._adviceFactory.Diagnostics;

        public T Target { get; }

        [Memo]
        public IAspectReceiver<T> Outbound
            => new RootAspectReceiver<T>(
                this.Target.ToRef(),
                this,
                CompilationModelVersion.Current );

        IDeclaration IAdviser.Target => this.Target;

        public void SkipAspect() => this._aspectBuilderState.AspectInstance.Skip();

        public bool IsAspectSkipped => this._aspectBuilderState.AspectInstance.IsSkipped;

        public IAspectState? AspectState
        {
            get => this.AspectInstance.AspectState;
            set => ((IAspectInstanceInternal) this.AspectInstance).SetState( value );
        }

        public CancellationToken CancellationToken => this._aspectBuilderState.CancellationToken;

        public bool VerifyEligibility( IEligibilityRule<T> rule )
        {
            var result = rule.GetEligibility( this.Target );

            switch ( result )
            {
                case EligibleScenarios.None:
                    {
                        var justification = rule.GetIneligibilityJustification( EligibleScenarios.Default, new DescribedObject<T>( this.Target ) );

                        this._aspectBuilderState.Diagnostics.Report(
                            GeneralDiagnosticDescriptors.AspectNotEligibleOnTarget.CreateRoslynDiagnostic(
                                this.Diagnostics.DefaultTargetLocation?.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, this.Target.DeclarationKind, this.Target, justification!),
                                this ) );

                        this.SkipAspect();

                        return false;
                    }

                case EligibleScenarios.Inheritance:
                    // If inheritance is allowed, we return false without reporting any error.

                    this.SkipAspect();

                    return false;

                default:
                    return true;
            }
        }

        public string? Layer => this._aspectBuilderState.Layer;

        IAspectBuilder<T1> IAspectBuilder.WithTarget<T1>( T1 newTarget ) => this.With( newTarget );

        public object? Tags
        {
            get => this._aspectBuilderState.Tags;
            set => this._aspectBuilderState.Tags = value;
        }

        IAspectBuilder<T1> IAspectBuilder<T>.WithTarget<T1>( T1 newTarget ) => this.With( newTarget );

        public IAspectBuilder<TNewTarget> With<TNewTarget>( TNewTarget declaration )
            where TNewTarget : class, IDeclaration
        {
            if ( declaration == this.Target )
            {
                return (IAspectBuilder<TNewTarget>) (object) this;
            }
            else
            {
                return new AspectBuilder<TNewTarget>(
                    declaration,
                    this._aspectBuilderState,
                    this._adviceFactory.WithDeclaration( declaration ),
                    this.AspectPredecessor );
            }
        }

        public AspectPredecessor AspectPredecessor { get; private set; }

        Type IAspectReceiverParent.Type => this.AspectInstance.AspectClass.Type;

        UserCodeInvoker IAspectReceiverParent.UserCodeInvoker => this._aspectBuilderState.Configuration.UserCodeInvoker;

        ProjectServiceProvider IAspectReceiverParent.ServiceProvider => this._aspectBuilderState.Configuration.ServiceProvider;

        BoundAspectClassCollection IAspectReceiverParent.AspectClasses => this._aspectBuilderState.Configuration.BoundAspectClasses;

        public MethodBasedReferenceValidatorDriver GetReferenceValidatorDriver( MethodInfo validateMethod )
            => ((IValidatorDriverFactory) this.AspectInstance.AspectClass).GetReferenceValidatorDriver( validateMethod );

        public ClassBasedReferenceValidatorDriver GetReferenceValidatorDriver( Type type )
            => ((IValidatorDriverFactory) this.AspectInstance.AspectClass).GetReferenceValidatorDriver( type );

        public DeclarationValidatorDriver GetDeclarationValidatorDriver( ValidatorDelegate<DeclarationValidationContext> validate )
            => ((IValidatorDriverFactory) this.AspectInstance.AspectClass).GetDeclarationValidatorDriver( validate );

        public LicenseVerifier? LicenseVerifier { get; }

        string IDiagnosticSource.DiagnosticSourceDescription => ((IAspectInstanceInternal) this.AspectInstance).DiagnosticSourceDescription;

        T IAdviser<T>.Target => this.Target;

        IAdviser<TNewDeclaration> IAdviser.With<TNewDeclaration>( TNewDeclaration declaration ) => this.With( declaration );
    }
}