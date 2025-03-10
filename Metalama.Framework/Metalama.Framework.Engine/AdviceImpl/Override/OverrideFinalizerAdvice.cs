// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Override;
// TODO: Check why this class is unused.
// ReSharper disable once UnusedType.Global

// Because we are using OverrideMethodAdvice, but that does not return a correct AdviceKind (#34372).

internal class OverrideFinalizerAdvice : OverrideMemberAdvice<IMethod, IMethod>
{
    private readonly BoundTemplateMethod _boundTemplate;

    public OverrideFinalizerAdvice( AdviceConstructorParameters<IMethod> parameters, BoundTemplateMethod boundTemplate )
        : base( parameters )
    {
        this._boundTemplate = boundTemplate;
    }

    public override AdviceKind AdviceKind => AdviceKind.OverrideFinalizer;

    protected override OverrideMemberAdviceResult<IMethod> Implement( in AdviceImplementationContext context )
    {
        // TODO: order should be self if the target is introduced on the same layer.
        context.AddTransformation( new OverrideFinalizerTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._boundTemplate ) );

        return this.CreateSuccessResult();
    }
}