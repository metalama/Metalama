// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Transformations;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// The result of a <see cref="PipelineStepsState"/>.
    /// </summary>
    internal sealed record PipelineStepsResult(
        CompilationModel FirstCompilation,
        CompilationModel LastCompilation,
        IReadOnlyCollection<ITransformation> Transformations,
        IReadOnlyCollection<IAspectInstance> InheritableAspectInstances,
        ImmutableUserDiagnosticList Diagnostics,
        IAspectSource OverflowAspectSource,
        IReadOnlyCollection<IExtensionPipelineContributor> ExtensionContributors,
        IReadOnlyCollection<AspectInstanceResult> AspectInstanceResults );
}