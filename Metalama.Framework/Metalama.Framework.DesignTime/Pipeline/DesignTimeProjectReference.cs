// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.Engine.Aspects;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Associates a <see cref="ProjectKey"/> and a <see cref="TransitiveAspectsManifest"/>.
/// </summary>
internal readonly struct DesignTimeProjectReference : IEquatable<DesignTimeProjectReference>
{
    public ITransitiveAspectsManifest? TransitiveAspectsManifest { get; }

    public ProjectKey ProjectKey { get; }

    public DesignTimeProjectReference(
        ProjectKey projectKey,
        ITransitiveAspectsManifest? transitiveAspectsManifest = null )
    {
        this.TransitiveAspectsManifest = transitiveAspectsManifest;
        this.ProjectKey = projectKey;
    }

    public bool Equals( DesignTimeProjectReference other )
        => Equals( this.TransitiveAspectsManifest, other.TransitiveAspectsManifest ) && this.ProjectKey.Equals( other.ProjectKey );

    public override bool Equals( object? obj ) => obj is DesignTimeProjectReference other && this.Equals( other );

    public override int GetHashCode() => HashCode.Combine( this.TransitiveAspectsManifest, this.ProjectKey );
}