// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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