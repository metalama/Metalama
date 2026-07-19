// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

internal sealed class DesignTimeProjectVersion : ITransitiveAspectManifestProvider
{
    private readonly ImmutableDictionary<ProjectKey, DesignTimeProjectReference> _references;

    public DesignTimeAspectPipelineStatus PipelineStatus { get; }

    public IProjectVersion ProjectVersion { get; }

    /// <summary>
    /// Gets the design-time extension collections of the referenced projects. Cross-version references contribute
    /// nothing here, because they carry no live result; their contribution travels in the serialized manifest
    /// instead, which the engine deserializes into this project's own compile-time copy.
    /// </summary>
    public IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> ReferencedExtensions
        => this._references.Values.Select( r => r.TransitiveAspectsManifest?.Extensions ).WhereNotNull();

    public DesignTimeProjectVersion(
        IProjectVersion projectVersion,
        IEnumerable<DesignTimeProjectReference> references,
        DesignTimeAspectPipelineStatus pipelineStatus )
    {
        this.ProjectVersion = projectVersion;
        this.PipelineStatus = pipelineStatus;
        this._references = references.ToImmutableDictionary( x => x.ProjectKey, x => x );
    }

    public SerializedTransitiveAspectManifest GetSerializedTransitiveAspectsManifest( Compilation compilation )
    {
        if ( this._references.TryGetValue( compilation.GetProjectKey(), out var reference ) )
        {
            return reference.SerializedTransitiveAspectManifest;
        }

        return default;
    }

    public bool TryGetReusableTransitiveAspectsManifest(
        Compilation compilation,
        [NotNullWhen( true )] out ITransitiveAspectsManifest? manifest,
        [NotNullWhen( true )] out AspectPipelineConfiguration? producerConfiguration )
    {
        // Only a same-version project reference carries a live result; a cross-version reference carries the
        // serialized manifest alone, and there is nothing to reuse. We also need the producer's configuration to
        // compare compile-time copies, so require it to be present.
        if ( this._references.TryGetValue( compilation.GetProjectKey(), out var reference )
             && reference.TransitiveAspectsManifest is { HasTransitiveAspectManifestContent: true, Configuration: { } configuration } result )
        {
            producerConfiguration = configuration;
            manifest = result.LiveTransitiveAspectManifest;

            return true;
        }

        manifest = null;
        producerConfiguration = null;

        return false;
    }
}