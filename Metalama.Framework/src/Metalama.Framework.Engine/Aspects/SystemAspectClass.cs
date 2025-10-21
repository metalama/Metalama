// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// This class can represent aspect types that are implemented in the current project, as opposed to user aspects.
/// </summary>
internal sealed class SystemAspectClass : IBoundAspectClass
{
    public AspectLayer Layer { get; }

    public string FullName => this.Type.FullName.AssertNotNull();

    public string ShortName { get; }

    string IAspectClass.DisplayName => this.ShortName;

    string? IAspectClass.Description => null;

    bool IAspectClass.IsAbstract => false;

    public bool? IsInheritable => false;

    public bool IsAttribute => false;

    public Type Type { get; }

    public EditorExperienceOptions EditorExperienceOptions => EditorExperienceOptions.Default;

    public SystemAspectClass( in ProjectServiceProvider serviceProvider, CompilationModel compilation, string shortName, Type type )
    {
        this.ShortName = shortName;
        this.Type = type;
        this.Layer = new AspectLayer( this, null, shortName );
        this.Layers = [this.Layer];
        this.AspectDriver = new AspectDriver( serviceProvider, this, compilation );
    }

    public IAspectDriver AspectDriver { get; }

    public Location? GetDiagnosticLocation( Compilation compilation ) => null;

    ImmutableArray<TemplateClass> IAspectClassImpl.TemplateClasses => ImmutableArray<TemplateClass>.Empty;

    SyntaxAnnotation IAspectClassImpl.GeneratedCodeAnnotation => throw new NotSupportedException();

    public ImmutableArray<AspectLayer> Layers { get; }

    EligibleScenarios IAspectClassImpl.GetEligibility( IDeclaration obj, bool isInheritable ) => EligibleScenarios.Default;

    EligibleScenarios IEligibilityRule<IDeclaration>.GetEligibility( IDeclaration obj ) => EligibleScenarios.Default;

    FormattableString IEligibilityRule<IDeclaration>.GetIneligibilityJustification(
        EligibleScenarios requestedEligibility,
        IDescribedObject<IDeclaration> describedObject )
        => throw new AssertionFailedException( "This aspect is always eligible." );

    public string DiagnosticSourceDescription => this.ShortName;

    IReadOnlyCollection<IAspectClassImpl> IAspectClassImpl.DescendantClassesAndSelf => Array.Empty<IAspectClassImpl>();
}