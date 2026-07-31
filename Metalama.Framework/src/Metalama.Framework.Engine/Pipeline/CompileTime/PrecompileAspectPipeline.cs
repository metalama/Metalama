// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Engine.Pipeline.CompileTime;

/// <summary>
/// A <see cref="CompileTimeAspectPipeline"/> for precompile scenarios: the WPF <c>MarkupCompilePass1</c> temporary
/// assembly (<see cref="ExecutionScenario.WpfPrecompile"/>) and the Razor <c>RazorCompileComponentDeclaration</c>
/// reference assembly (<see cref="ExecutionScenario.RazorPrecompile"/>). Reuses the full compile-time front-end but
/// emits aspect-introduced member signatures only, skipping the linker, because the assembly is consumed only for
/// type resolution and then discarded.
/// </summary>
/// <remarks>
/// The two scenarios share a single implementation because they differ only in the <see cref="ExecutionScenario"/>
/// passed to the base pipeline (kept distinct so logs and crash reports identify the pass).
/// </remarks>
public sealed class PrecompileAspectPipeline : CompileTimeAspectPipeline
{
    public PrecompileAspectPipeline( ProjectServiceProvider serviceProvider, ExecutionScenario executionScenario )
        : base( serviceProvider, executionScenario ) { }

    private protected override HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new PrecompilePipelineStage( configuration.AspectLayers );
}
