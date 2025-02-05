// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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