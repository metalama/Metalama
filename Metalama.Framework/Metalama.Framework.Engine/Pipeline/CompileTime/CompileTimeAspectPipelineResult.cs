// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using System.Collections.Immutable;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Metalama.Framework.Engine.Pipeline.CompileTime
{
    public sealed record CompileTimeAspectPipelineResult(
        ImmutableArray<SyntaxTreeTransformation> SyntaxTreeTransformations,
        ImmutableArray<ManagedResource> AdditionalResources,
        IPartialCompilation ResultingCompilation,
        ImmutableArray<AdditionalCompilationOutputFile> AdditionalCompilationOutputFiles,
        ImmutableArray<ScopedSuppression> DiagnosticSuppressions,
        AspectPipelineConfiguration? Configuration );
}