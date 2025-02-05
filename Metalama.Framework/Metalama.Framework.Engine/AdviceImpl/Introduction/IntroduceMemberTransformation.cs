// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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