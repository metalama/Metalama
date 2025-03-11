// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.Transformations;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal abstract class IntroduceMemberTransformation<T> : IntroduceDeclarationTransformation<T>
    where T : MemberBuilderData
{
    protected IntroduceMemberTransformation( AspectLayerInstance aspectLayerInstance, T introducedDeclaration ) : base(
        aspectLayerInstance,
        introducedDeclaration ) { }

    public override TransformationObservability Observability
        => this.BuilderData.IsDesignTimeObservable ? TransformationObservability.Always : TransformationObservability.CompileTimeOnly;
}