// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Identifies the kind of compilation Metalama is participating in. Settable via the
/// <c>MetalamaCompilationScenario</c> MSBuild property and exposed as <see cref="IProjectOptions.CompilationScenario"/>.
/// Most builds use <see cref="Default"/>; specific values exist for build contexts that need a tailored pipeline.
/// </summary>
public enum CompilationScenario
{
    /// <summary>
    /// The standard compile-time pipeline: front-end + linker, producing a fully transformed assembly.
    /// </summary>
    Default = 0,

    /// <summary>
    /// The temporary assembly produced by WPF's <c>MarkupCompilePass1</c> (project name suffix <c>_wpftmp</c>).
    /// The pipeline emits aspect-introduced member signatures only; the linker is skipped because the assembly
    /// is consumed only by the XAML compiler for type resolution and then discarded.
    /// </summary>
    WpfPrecompile = 1,
}
