// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Design-time implementation of <see cref="IUpstreamCompileTimeProjectProvider"/> (issue #1611).
/// When a downstream pipeline's <see cref="CompileTimeProjectRepository.Builder"/> walks references and
/// encounters a <see cref="CompilationReference"/>, this provider returns the upstream pipeline's
/// already-built <see cref="AspectPipelineConfiguration"/> (whose <c>CompileTimeProject</c> the Builder
/// then reuses) instead of forcing a fresh recursive build that would yield a different physical
/// projection of the same logical upstream.
/// Returns <see langword="false"/> in three guard cases — see method body.
/// </summary>
internal sealed class DesignTimeUpstreamCompileTimeProjectProvider : IUpstreamCompileTimeProjectProvider
{
    private readonly DesignTimeAspectPipelineFactory _pipelineFactory;

    public DesignTimeUpstreamCompileTimeProjectProvider( GlobalServiceProvider serviceProvider )
    {
        this._pipelineFactory = serviceProvider.GetRequiredService<DesignTimeAspectPipelineFactory>();
    }

    public bool TryGetUpstreamConfiguration( Compilation compilation, [NotNullWhen( true )] out AspectPipelineConfiguration? configuration )
    {
        // 1) Cross-version guard: reusing across Metalama versions would yield types from a different
        //    Metalama.Framework.Engine assembly, which is invalid. Fall back to recursive build, which
        //    will (correctly) build a CompileTimeProject for the current Metalama version.
        if ( !this._pipelineFactory.TryGetMetalamaVersion( compilation, out var version )
             || version != EngineAssemblyMetadataReader.Instance.AssemblyVersion )
        {
            configuration = null;

            return false;
        }

        // 2) Existing-pipeline guard: only reuse if a pipeline for this compilation has already been
        //    created. We deliberately avoid GetOrCreatePipelineAsync to stay synchronous.
        if ( !this._pipelineFactory.TryGetPipeline( compilation.GetProjectKey(), out var pipeline ) )
        {
            configuration = null;

            return false;
        }

        // 3) Configuration-ready guard: only reuse if the upstream pipeline has computed a successful
        //    AspectPipelineConfiguration. CurrentConfiguration returns null otherwise.
        configuration = pipeline.CurrentConfiguration;

        return configuration != null;
    }
}
