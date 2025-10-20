// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed partial class IntroduceConstructorParameterTransitiveAspect : IAspect<INamedType>
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly IReadOnlyList<IRef<IParameter>> _parameters;
    private readonly int _order;

    public IntroduceConstructorParameterTransitiveAspect(
        IPullStrategy? pullStrategy,
        IReadOnlyList<IRef<IParameter>> parameters,
        int order )
    {
        this._pullStrategy = pullStrategy;
        this._parameters = parameters;
        this._order = order;
    }

    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var allInstances = builder.AspectInstance.SecondaryInstances.Select( x => (IntroduceConstructorParameterTransitiveAspect) x.Aspect )
            .Concat( this )
            .OrderBy( a => a._order );

        var internalBuilder = (AspectBuilder<INamedType>) builder;

        foreach ( var instance in allInstances )
        {
            foreach ( var parameterRef in instance._parameters )
            {
                var parameter = parameterRef.GetTarget( internalBuilder.AdviceFactory.MutableCompilation );
                internalBuilder.AdviceFactory.PullParameter( parameter, this._pullStrategy );
            }
        }
    }

    public class AspectClass : IAspectClassImpl
    {
        public static AspectClass Instance { get; } = new();

        private AspectClass() { }

        string IAspectClass.FullName => typeof(IntroduceConstructorParameterTransitiveAspect).FullName.AssertNotNull();

        string IAspectClass.ShortName => nameof(IntroduceConstructorParameterTransitiveAspect);

        public string DisplayName => "Pulling constructor parameters from referenced projects";

        string? IAspectClass.Description => null;

        bool IAspectClass.IsAbstract => false;

        bool? IAspectClass.IsInheritable => false;

        bool IAspectClass.IsAttribute => false;

        Type IAspectClass.Type => typeof(IntroduceConstructorParameterTransitiveAspect);

        EditorExperienceOptions IAspectClass.EditorExperienceOptions => EditorExperienceOptions.Default;

        EligibleScenarios IEligibilityRule<IDeclaration>.GetEligibility( IDeclaration obj ) => EligibleScenarios.None;

        FormattableString? IEligibilityRule<IDeclaration>.GetIneligibilityJustification(
            EligibleScenarios requestedEligibility,
            IDescribedObject<IDeclaration> describedObject )
            => null;

        string IDiagnosticSource.DiagnosticSourceDescription => this.DisplayName;

        ImmutableArray<TemplateClass> IAspectClassImpl.TemplateClasses => [];

        SyntaxAnnotation IAspectClassImpl.GeneratedCodeAnnotation => throw new NotImplementedException();

        ImmutableArray<AspectLayer> IAspectClassImpl.Layers { get; } =
            ImmutableArray.Create( new AspectLayer( $"<{nameof(IntroduceConstructorParameterTransitiveAspect)}>", null ) );

        EligibleScenarios IAspectClassImpl.GetEligibility( IDeclaration obj, bool isInheritable ) => EligibleScenarios.All;

        IReadOnlyCollection<IAspectClassImpl> IAspectClassImpl.DescendantClassesAndSelf => [this];
    }
}