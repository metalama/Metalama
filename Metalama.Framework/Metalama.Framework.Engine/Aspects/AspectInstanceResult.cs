// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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