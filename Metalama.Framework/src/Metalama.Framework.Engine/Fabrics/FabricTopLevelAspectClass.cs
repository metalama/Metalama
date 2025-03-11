// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Fabrics;

/// <summary>
/// The top-level aspect class integrating the fabrics feature in the aspect pipeline. It is used as an 'identity'
/// class. The real class is <see cref="FabricAggregateAspectClass"/>, which is instantiated in the middle of the pipeline,
/// while <see cref="FabricTopLevelAspectClass"/> must exist while the pipeline is being instantiated.
/// </summary>
internal sealed class FabricTopLevelAspectClass : IBoundAspectClass
{
    public const string FabricAspectName = "<Fabric>";

    public AspectLayer Layer { get; }

    string IAspectClass.FullName => FabricAspectName;

    public string ShortName => FabricAspectName;

    string IAspectClass.DisplayName => FabricAspectName;

    string? IAspectClass.Description => null;

    bool IAspectClass.IsAbstract => false;

    public bool? IsInheritable => false;

    public bool IsAttribute => false;

    public Type Type => typeof(Fabric);

    public EditorExperienceOptions EditorExperienceOptions => EditorExperienceOptions.Default;

    public FabricTopLevelAspectClass( in ProjectServiceProvider serviceProvider, CompilationModel compilation )
    {
        this.Layer = new AspectLayer( this, null );
        this.AspectDriver = new AspectDriver( serviceProvider, this, compilation );
    }

    public IAspectDriver AspectDriver { get; }

    public Location? GetDiagnosticLocation( Compilation compilation ) => null;

    ImmutableArray<TemplateClass> IAspectClassImpl.TemplateClasses => ImmutableArray<TemplateClass>.Empty;

    SyntaxAnnotation IAspectClassImpl.GeneratedCodeAnnotation => throw new NotSupportedException();

    public ImmutableArray<AspectLayer> Layers { get; } = ImmutableArray.Create( new AspectLayer( "<Fabric>", null ) );

    EligibleScenarios IAspectClassImpl.GetEligibility( IDeclaration obj, bool isInheritable ) => EligibleScenarios.Default;

    EligibleScenarios IEligibilityRule<IDeclaration>.GetEligibility( IDeclaration obj ) => EligibleScenarios.Default;

    FormattableString IEligibilityRule<IDeclaration>.GetIneligibilityJustification(
        EligibleScenarios requestedEligibility,
        IDescribedObject<IDeclaration> describedObject )
        => throw new AssertionFailedException( "This aspect is always eligible." );

    public string DiagnosticSourceDescription => "top-level fabric";

    IReadOnlyCollection<IAspectClassImpl> IAspectClassImpl.DescendantClassesAndSelf => Array.Empty<IAspectClassImpl>();
}