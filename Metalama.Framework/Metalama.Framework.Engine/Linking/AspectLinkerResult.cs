// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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