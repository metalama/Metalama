// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility;

public sealed record ExtensionPipelineContributorsResult(
    ImmutableArray<ITransitivePipelineContributor> TransitiveContributors,
    ImmutableUserDiagnosticList Diagnostics )
{
    public static ExtensionPipelineContributorsResult Empty { get; } = new(
        ImmutableArray<ITransitivePipelineContributor>.Empty,
        ImmutableUserDiagnosticList.Empty );

    private bool IsEmpty => this.TransitiveContributors.IsEmpty && this.Diagnostics.IsEmpty;

    public ExtensionPipelineContributorsResult Concat( ExtensionPipelineContributorsResult other )
    {
        if ( this.IsEmpty )
        {
            return other;
        }
        else if ( other.IsEmpty )
        {
            return this;
        }
        else
        {
            return new ExtensionPipelineContributorsResult(
                this.TransitiveContributors.AddRange( other.TransitiveContributors ),
                this.Diagnostics.Concat( other.Diagnostics ) );
        }
    }
}