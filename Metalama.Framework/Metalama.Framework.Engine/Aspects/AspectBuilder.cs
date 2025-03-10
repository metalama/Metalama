// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;
using System.Threading;

namespace Metalama.Framework.Engine.Aspects
{
    internal sealed class AspectBuilder<T> : IAspectBuilder<T>, IAspectBuilderInternal, IQueryOwner
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
        }

        public IProject Project => this.Target.Compilation.Project;

        [Memo]
        public string? Namespace => this.Target.GetNamespace()?.FullName;

        public IAspectInstance AspectInstance => this._aspectBuilderState.AspectInstance;

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
        public T AdvisedTarget => this.Target.ForCompilation( this._adviceFactory.MutableCompilation );

        [Memo]
        public IQuery<T> Outbound
            => new RootQuery<T>(
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

        Type IQueryOwner.Type => this.AspectInstance.AspectClass.Type;

        public UserCodeExecutionContext UserCodeExecutionContext => this._aspectBuilderState.UserCodeExecutionContext;

        UserCodeInvoker IQueryOwner.UserCodeInvoker => this._aspectBuilderState.Configuration.UserCodeInvoker;

        ProjectServiceProvider IQueryOwner.ServiceProvider => this._aspectBuilderState.Configuration.ServiceProvider;

        IAspectClassResolver IQueryOwner.AspectClasses => this._aspectBuilderState.Configuration.BoundAspectClasses;

        string IDiagnosticSource.DiagnosticSourceDescription => ((IAspectInstanceInternal) this.AspectInstance).DiagnosticSourceDescription;

        T IAdviser<T>.Target => this.Target;

        IAdviser<TNewDeclaration> IAdviser.With<TNewDeclaration>( TNewDeclaration declaration ) => this.With( declaration );

        public void AddContributor( IPipelineContributor contributor ) => this._aspectBuilderState.AddContributor( contributor );
    }
}