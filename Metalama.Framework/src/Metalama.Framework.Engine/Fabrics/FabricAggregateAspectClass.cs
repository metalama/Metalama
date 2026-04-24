// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
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
#pragma warning disable IDE1006 // Naming rule violation: intentional angle-bracketed name mirrors CLR compiler-generated identifier
        private const string _aspectClassName = "<Fabric>";
#pragma warning restore IDE1006

        public static IBoundAspectClass CreateTopLevelAspectClass( in ProjectServiceProvider serviceProvider, CompilationModel compilation )
            => new SystemAspectClass( serviceProvider, compilation, _aspectClassName, typeof(Fabric) );

        public FabricAggregateAspectClass( ImmutableArray<TemplateClass> templateClasses )
        {
            this.TemplateClasses = templateClasses;

            var description = "fabric " + string.Join( " or ", templateClasses.Select( x => x.FullName ) );

            this.GeneratedCodeAnnotation =
                MetalamaCompilerAnnotations.CreateGeneratedCodeAnnotation( description );

            this.DiagnosticSourceDescription = description;
        }

        public string FullName => typeof(Fabric).FullName.AssertNotNull();

        public string ShortName => _aspectClassName;

        public string DisplayName => _aspectClassName;

        public string? Description => null;

        public bool IsAbstract => false;

        public bool? IsInheritable => false;

        public bool IsAttribute => false;

        public Type Type => typeof(Fabric);

        public EditorExperienceOptions EditorExperienceOptions => EditorExperienceOptions.Default;

        public ImmutableArray<TemplateClass> TemplateClasses { get; }

        public SyntaxAnnotation GeneratedCodeAnnotation { get; }

        public ImmutableArray<AspectLayer> Layers { get; } = ImmutableArray.Create( new AspectLayer( _aspectClassName, null ) );

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