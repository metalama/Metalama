// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideMethodAdvice : OverrideMemberAdvice<IMethod, IMethod>
{
    private readonly BoundTemplateMethod _boundTemplate;

    public OverrideMethodAdvice( AdviceConstructorParameters<IMethod> parameters, BoundTemplateMethod boundTemplate )
        : base( parameters )
    {
        this._boundTemplate = boundTemplate;
    }

    public override AdviceKind AdviceKind => AdviceKind.OverrideMethod;

    protected override OverrideMemberAdviceResult<IMethod> Implement( in AdviceImplementationContext context )
    {
        // TODO: order should be self if the target is introduced on the same layer.
        var targetMethod = this.TargetDeclaration;

        switch ( targetMethod.MethodKind )
        {
            case MethodKind.Finalizer:
                context.AddTransformation(
                    new OverrideFinalizerTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._boundTemplate ) );

                break;

            case MethodKind.Operator:
                context.AddTransformation(
                    new OverrideOperatorTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._boundTemplate ) );

                break;

            default:
                context.AddTransformation(
                    new OverrideMethodTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._boundTemplate ) );

                break;
        }

        return this.CreateSuccessResult( targetMethod );
    }
}