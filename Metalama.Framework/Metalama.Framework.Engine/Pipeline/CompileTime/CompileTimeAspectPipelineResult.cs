// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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