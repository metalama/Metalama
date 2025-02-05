// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

internal sealed class DesignTimeProjectVersion : ITransitiveAspectManifestProvider
{
    private readonly ImmutableDictionary<ProjectKey, DesignTimeProjectReference> _references;

    public DesignTimeAspectPipelineStatus PipelineStatus { get; }

    public IProjectVersion ProjectVersion { get; }

    public IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> ReferencedExtensions
        => this._references.Values.Select( r => (r.TransitiveAspectsManifest as DesignTimeAspectPipelineResult)?.Extensions ).WhereNotNull();

    public DesignTimeProjectVersion(
        IProjectVersion projectVersion,
        IEnumerable<DesignTimeProjectReference> references,
        DesignTimeAspectPipelineStatus pipelineStatus )
    {
        this.ProjectVersion = projectVersion;
        this.PipelineStatus = pipelineStatus;
        this._references = references.ToImmutableDictionary( x => x.ProjectKey, x => x );
    }

    public ITransitiveAspectsManifest? GetTransitiveAspectsManifest( Compilation compilation )
    {
        if ( this._references.TryGetValue( compilation.GetProjectKey(), out var reference ) )
        {
            return reference.TransitiveAspectsManifest;
        }

        return null;
    }
}