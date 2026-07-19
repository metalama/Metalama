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
    /// Gets the design-time extension collections of the referenced projects. This is one of the two channels by
    /// which a reference's design-time validators reach this project, and it serves same-version references only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A reference built against a different version of Metalama contributes nothing here, because it carries no
    /// live result to read the collection from. It is not lost: it travels instead through the serialized manifest,
    /// which <c>TransitivePipelineContributorSource</c> deserializes into this project's own compile-time copy. That
    /// is why <c>DesignTimeAspectPipelineResult.CreateTransitiveManifest</c> puts validators in the manifest for a
    /// cross-version consumer and keeps them out for a same-version one: whichever channel is unavailable, exactly
    /// one carries them.
    /// </para>
    /// <para>
    /// The split is not merely an optimization. This channel deduplicates diamond-shaped reference graphs, whereas
    /// the manifest channel is walked once per direct reference and does not, so routing a same-version reference
    /// through both delivers its validators more than once.
    /// </para>
    /// </remarks>
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