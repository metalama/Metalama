// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Transformations;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Aspects
{
    internal sealed class AspectInstanceResult
    {
        public IAspectInstance AspectInstance { get; }

        public AdviceOutcome Outcome { get; }

        public ImmutableUserDiagnosticList Diagnostics { get; }

        public ImmutableArray<ITransformation> Transformations { get; }

        public ImmutableArray<IPipelineContributor> Contributors { get; }

        public AspectInstanceResult(
            IAspectInstance aspectInstance,
            AdviceOutcome outcome,
            ImmutableUserDiagnosticList diagnostics,
            ImmutableArray<ITransformation> transformations,
            ImmutableArray<IPipelineContributor> contributors )
        {
            this.AspectInstance = aspectInstance;
            this.Outcome = outcome;
            this.Diagnostics = diagnostics;
            this.Transformations = transformations;
            this.Contributors = contributors;
        }

        public AspectInstanceResult WithAdditionalDiagnostics( ImmutableUserDiagnosticList diagnostics )
            => new(
                this.AspectInstance,
                this.Outcome,
                this.Diagnostics.Concat( diagnostics ),
                this.Transformations,
                this.Contributors );
    }
}