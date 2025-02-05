// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Fabrics
{
    /// <summary>
    /// An aspect class that aggregates all fabrics on a given declaration.
    /// </summary>
    internal sealed class FabricAggregateAspectClass : IAspectClassImpl
    {
        public FabricAggregateAspectClass( ImmutableArray<TemplateClass> templateClasses )
        {
            this.TemplateClasses = templateClasses;

            var description = "fabric " + string.Join( " or ", templateClasses.Select( x => x.FullName ) );

            this.GeneratedCodeAnnotation =
                MetalamaCompilerAnnotations.CreateGeneratedCodeAnnotation( description );

            this.DiagnosticSourceDescription = description;
        }

        public string FullName => FabricTopLevelAspectClass.FabricAspectName;

        public string ShortName => FabricTopLevelAspectClass.FabricAspectName;

        public string DisplayName => FabricTopLevelAspectClass.FabricAspectName;

        public string? Description => null;

        public bool IsAbstract => false;

        public bool? IsInheritable => false;

        public bool IsAttribute => false;

        public Type Type => typeof(Fabric);

        public EditorExperienceOptions EditorExperienceOptions => EditorExperienceOptions.Default;
        
        public ImmutableArray<TemplateClass> TemplateClasses { get; }

        public SyntaxAnnotation GeneratedCodeAnnotation { get; }

        public ImmutableArray<AspectLayer> Layers { get; } = ImmutableArray.Create( new AspectLayer( "<Fabric>", null ) );

        EligibleScenarios IAspectClassImpl.GetEligibility( IDeclaration obj, bool isInheritable ) => EligibleScenarios.Default;

        IReadOnlyCollection<IAspectClassImpl> IAspectClassImpl.DescendantClassesAndSelf => Array.Empty<IAspectClassImpl>();

        EligibleScenarios IEligibilityRule<IDeclaration>.GetEligibility( IDeclaration obj ) => EligibleScenarios.Default;

        FormattableString IEligibilityRule<IDeclaration>.GetIneligibilityJustification(
            EligibleScenarios requestedEligibility,
            IDescribedObject<IDeclaration> describedObject )
            => throw new AssertionFailedException( "This method should not be called because it is always eligible." );

        public string DiagnosticSourceDescription { get; }
    }
}