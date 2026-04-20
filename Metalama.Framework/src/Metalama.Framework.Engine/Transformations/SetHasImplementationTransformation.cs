// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Introspection;
using System;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// A transformation that marks a member as having an implementation. This is used when an override
/// provides an implementation for a member that previously did not have one (e.g., a partial method
/// without an implementation part).
/// </summary>
internal sealed class SetHasImplementationTransformation : BaseTransformation
{
    public SetHasImplementationTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IMember> targetMember ) : base( aspectLayerInstance )
    {
        this.TargetMember = targetMember;
    }

    public IFullRef<IMember> TargetMember { get; }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMember;

    public override TransformationObservability Observability => TransformationObservability.CompileTimeOnly;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.OverrideMember;

    public override FormattableString ToDisplayString() => $"Set HasImplementation on '{this.TargetMember.Definition.ToDisplayString()}'";
}