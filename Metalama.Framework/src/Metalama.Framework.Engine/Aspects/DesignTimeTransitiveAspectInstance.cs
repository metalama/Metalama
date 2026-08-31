// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// The design-time form of a <see cref="TransitiveAspectInstance"/>.
/// </summary>
/// <remarks>
/// <para>
/// A separate object rather than the contributor itself. The contributor is produced by one run and is free to hold
/// whatever that run has at hand; this is stored per file by the design-time pipeline and carried forward across
/// edits, so it must be durable. When one class was both, the stricter requirement applied to state that only the
/// looser one needed, and the way out was to leave the offending member unsuppressed.
/// </para>
/// <para>
/// It carries only what <see cref="SerializableTransitiveAspectInstance"/> reads, which is why the aspect class is
/// kept as its name rather than as an <see cref="IAspectClassImpl"/>.
/// </para>
/// </remarks>
internal sealed class DesignTimeTransitiveAspectInstance : IDesignTimePipelineResultExtension
{
    public DesignTimeTransitiveAspectInstance( TransitiveAspectInstance contributor )
    {
        this.Aspect = contributor.Aspect;
        this.AspectClassName = contributor.AspectClass.FullName;
        this.AspectState = contributor.AspectState;
        this.TargetDeclaration = contributor.TargetDeclaration;
        this.TargetDeclarationDepth = contributor.TargetDeclarationDepth;
    }

    public IAspect Aspect { get; }

    public string AspectClassName { get; }

    public IAspectState? AspectState { get; }

    public IDurableRef<IDeclaration> TargetDeclaration { get; }

    public int TargetDeclarationDepth { get; }

    public ContributorKind ContributorKind => ContributorKind.TransitiveAspectInstance;

    public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension()
        => new SerializableTransitiveAspectInstance(
            this.Aspect,
            this.AspectClassName,
            this.AspectState,
            this.TargetDeclaration,
            this.TargetDeclarationDepth );
}
