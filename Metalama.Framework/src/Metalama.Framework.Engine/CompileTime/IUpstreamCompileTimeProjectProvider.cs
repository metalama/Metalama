// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Cross-pipeline lookup of an already-configured upstream pipeline's <see cref="AspectPipelineConfiguration"/>
/// for a given <see cref="Compilation"/>. Used by <see cref="CompileTimeProjectRepository.Builder"/> when it
/// encounters a <see cref="CompilationReference"/> to a project whose own pipeline has already been initialised
/// — for example, when project A and project B both have running design-time pipelines and B is referenced as a
/// project by A. Reusing the upstream's compile-time project ensures both pipelines share the same physical
/// loaded assembly for B, eliminating the cross-binding scenario where <c>IAspectClass.Type</c> and a
/// deserialised or live <c>IAspect</c> are bound to two different physical projections of the same logical
/// upstream (issue #1611).
///
/// The interface exposes <see cref="AspectPipelineConfiguration"/> (a public type) rather than
/// <see cref="CompileTimeProject"/> (internal) so the design-time implementation can sit in a different
/// assembly without requiring an <c>InternalsVisibleTo</c> exception.
///
/// The default <see cref="NullUpstreamCompileTimeProjectProvider"/> implementation returns <see langword="false"/>
/// for all inputs; build-time and unit-test scenarios fall through to the existing recursive build in
/// <see cref="CompileTimeProjectRepository.Builder"/>. The design-time host overrides this with an implementation
/// that consults the running pipelines.
/// </summary>
public interface IUpstreamCompileTimeProjectProvider : IGlobalService
{
    bool TryGetUpstreamConfiguration( Compilation compilation, [NotNullWhen( true )] out AspectPipelineConfiguration? configuration );
}

internal sealed class NullUpstreamCompileTimeProjectProvider : IUpstreamCompileTimeProjectProvider
{
    public static NullUpstreamCompileTimeProjectProvider Instance { get; } = new();

    public bool TryGetUpstreamConfiguration( Compilation compilation, [NotNullWhen( true )] out AspectPipelineConfiguration? configuration )
    {
        configuration = null;

        return false;
    }
}
