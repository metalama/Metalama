// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Result of the aspect linker.
/// </summary>
internal sealed record AspectLinkerResult
{
    /// <summary>
    /// Gets the final compilation.
    /// </summary>
    public PartialCompilation Compilation { get; }

    /// <summary>
    /// Gets diagnostics produced when linking (template expansion, inlining, etc.).
    /// </summary>
    public ImmutableUserDiagnosticList Diagnostics { get; }

    public AspectLinkerResult( PartialCompilation compilation, ImmutableUserDiagnosticList diagnostics )
    {
        this.Compilation = compilation;
        this.Diagnostics = diagnostics;
    }
}