// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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