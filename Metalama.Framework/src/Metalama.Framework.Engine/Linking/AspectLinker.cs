// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Transforms the initial C# compilation using all transformations and aspect ordering determined in earlier stages.
/// </summary>
internal sealed class AspectLinker
{
    private readonly CompilationContext _compilationContext;
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly AspectLinkerInput _input;

    public AspectLinker( in ProjectServiceProvider serviceProvider, AspectLinkerInput input )
    {
        this._compilationContext = input.FinalCompilationModel.CompilationContext;
        this._serviceProvider = serviceProvider;
        this._input = input;
    }

    /// <summary>
    /// Creates a set of diagnostics and final linked compilation.
    /// </summary>
    /// <returns>Linker result.</returns>
    public async Task<AspectLinkerResult> ExecuteAsync( CancellationToken cancellationToken )
    {
        // First step. Adds all transformations to the compilation, resulting in intermediate compilation.
        var injectionStepOutput =
            await new LinkerInjectionStep( this._serviceProvider, this._compilationContext ).ExecuteAsync( this._input, cancellationToken );

        // Second step. Count references to modified methods on semantic models of intermediate compilation and analyze method bodies.
        var analysisStepOutput =
            await new LinkerAnalysisStep( this._serviceProvider ).ExecuteAsync( injectionStepOutput, cancellationToken );

        // Third step. Link, inline and prune intermediate compilation. This results in the final compilation.
        var linkingStepOutput = await new LinkerLinkingStep( this._serviceProvider ).ExecuteAsync( analysisStepOutput, cancellationToken );

        // Return the final compilation and all diagnostics from all linking steps.
        return linkingStepOutput;
    }
}