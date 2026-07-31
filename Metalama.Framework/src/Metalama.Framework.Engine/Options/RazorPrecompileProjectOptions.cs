// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// An <see cref="IProjectOptions"/> that forces <see cref="CompilationScenario.RazorDeclaration"/>. Applied when the
/// compiler is detected to be running the Razor <c>RazorCompileComponentDeclaration</c> pass, which cannot signal the
/// scenario through the <c>MetalamaCompilationScenario</c> MSBuild property because that pass forwards no
/// <c>/analyzerconfig</c>. See issue #1741.
/// </summary>
internal sealed class RazorPrecompileProjectOptions : ProjectOptionsWrapper
{
    public RazorPrecompileProjectOptions( IProjectOptions wrapped ) : base( wrapped ) { }

    public override CompilationScenario CompilationScenario => CompilationScenario.RazorDeclaration;
}
