// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
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

    /// <remarks>
    /// The diagnostics hold no syntax tree, so their locations name a file but point into no text. This is enough for
    /// a caller that reads the identifier or the message, which is what the tests of the pipeline do. A caller that
    /// reports a diagnostic uses <see cref="GetDiagnosticsOnSyntaxTree"/> instead, which binds it to a tree.
    /// </remarks>
    internal IEnumerable<Diagnostic> GetAllDiagnostics()
        => this.Result.SyntaxTreeResults.SelectMany( x => x.Value.Diagnostics ).Select( d => d.ToDiagnostic( null ) );

    internal ImmutableArray<CacheableScopedSuppression> GetSuppressionsOnSyntaxTree( DocumentKey documentKey )
    {
        if ( this.Result.SyntaxTreeResults.TryGetValue( documentKey, out var syntaxTreeResult ) )
        {
            return syntaxTreeResult.Suppressions;
        }
        else
        {
            return ImmutableArray<CacheableScopedSuppression>.Empty;
        }
    }

    /// <summary>
    /// Returns the diagnostics of a document, with their locations bound to a syntax tree.
    /// </summary>
    /// <param name="documentKey">The document whose diagnostics are wanted.</param>
    /// <param name="syntaxTree">
    /// The tree of that document in the compilation being analysed. The diagnostics are stored without a tree, and
    /// they all belong to this document, so one tree binds all of them.
    /// </param>
    public ImmutableArray<Diagnostic> GetDiagnosticsOnSyntaxTree( DocumentKey documentKey, SyntaxTree? syntaxTree )
    {
        if ( this.Result.SyntaxTreeResults.TryGetValue( documentKey, out var syntaxTreeResult ) )
        {
            return syntaxTreeResult.Diagnostics.SelectAsImmutableArray( d => d.ToDiagnostic( syntaxTree ) );
        }
        else
        {
            return ImmutableArray<Diagnostic>.Empty;
        }
    }
}