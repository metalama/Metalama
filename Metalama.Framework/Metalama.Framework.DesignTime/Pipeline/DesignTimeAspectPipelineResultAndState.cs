// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.Engine.Pipeline;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

public sealed class DesignTimeAspectPipelineResultAndState
{
    public DesignTimeAspectPipelineResult Result { get; }

    public AspectPipelineConfiguration Configuration { get; }

    internal ProjectVersion ProjectVersion { get; }

    internal DesignTimeAspectPipelineStatus Status { get; }

    internal DesignTimeAspectPipelineResultAndState(
        ProjectVersion projectVersion,
        DesignTimeAspectPipelineResult result,
        DesignTimeAspectPipelineStatus status,
        AspectPipelineConfiguration configuration )
    {
        this.Status = status;
        this.Configuration = configuration;
        this.Result = result;
        this.ProjectVersion = projectVersion;
    }

    internal IEnumerable<Diagnostic> GetAllDiagnostics() => this.Result.SyntaxTreeResults.SelectMany( x => x.Value.Diagnostics );

    internal ImmutableArray<CacheableScopedSuppression> GetSuppressionsOnSyntaxTree( string path )
    {
        if ( this.Result.SyntaxTreeResults.TryGetValue( path, out var syntaxTreeResult ) )
        {
            return syntaxTreeResult.Suppressions;
        }
        else
        {
            return ImmutableArray<CacheableScopedSuppression>.Empty;
        }
    }

    public ImmutableArray<Diagnostic> GetDiagnosticsOnSyntaxTree( string path )
    {
        if ( this.Result.SyntaxTreeResults.TryGetValue( path, out var syntaxTreeResult ) )
        {
            return syntaxTreeResult.Diagnostics;
        }
        else
        {
            return ImmutableArray<Diagnostic>.Empty;
        }
    }
}