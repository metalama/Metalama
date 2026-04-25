// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Engine.Pipeline.CompileTime;

/// <summary>
/// A <see cref="CompileTimeAspectPipeline"/> for the WPF MarkupCompilePass1 temporary assembly. Reuses the full
/// compile-time front-end but emits aspect-introduced member signatures only — the linker is skipped because the
/// temp assembly is consumed only by the XAML compiler for type resolution and then discarded.
/// </summary>
public sealed class WpfPrecompileAspectPipeline : CompileTimeAspectPipeline
{
    public WpfPrecompileAspectPipeline( ProjectServiceProvider serviceProvider )
        : base( serviceProvider, ExecutionScenario.WpfPrecompile ) { }

    private protected override HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new WpfPrecompilePipelineStage( configuration.AspectLayers );
}
