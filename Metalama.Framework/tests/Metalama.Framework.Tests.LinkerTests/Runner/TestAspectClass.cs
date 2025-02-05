// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.LinkerTests.Runner;

internal sealed class TestAspectClass : IAspectClassImpl
{
    public CompileTimeProject Project => throw new NotImplementedException();

    public ImmutableArray<TemplateClass> TemplateClasses => throw new NotImplementedException();

    public SyntaxAnnotation GeneratedCodeAnnotation { get; }

    public ImmutableArray<AspectLayer> Layers => throw new NotImplementedException();

    public IReadOnlyCollection<IAspectClassImpl> DescendantClassesAndSelf => throw new NotImplementedException();

    public string FullName { get; }

    public string ShortName => this.FullName;

    public string DisplayName => this.FullName;

    public string Description => throw new NotImplementedException();

    public bool IsAbstract => throw new NotImplementedException();

    public bool? IsInheritable => false;

    public bool IsAttribute => throw new NotImplementedException();

    public Type Type => throw new NotImplementedException();

    public EditorExperienceOptions EditorExperienceOptions => throw new NotImplementedException();

    public string DiagnosticSourceDescription => throw new NotImplementedException();

    public TestAspectClass( string aspectName )
    {
        this.FullName = aspectName;
        this.GeneratedCodeAnnotation = MetalamaCompilerAnnotations.CreateGeneratedCodeAnnotation( $"aspect '{this.ShortName}'" );
    }

    public EligibleScenarios GetEligibility( IDeclaration obj, bool isInheritable ) => throw new NotImplementedException();

    public EligibleScenarios GetEligibility( IDeclaration obj ) => throw new NotImplementedException();

    public FormattableString GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
        => throw new NotImplementedException();
}
