// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideMethodAdvice : OverrideMemberAdvice<IMethod, IMethod>
{
    private readonly BoundTemplateMethod _boundTemplate;

    public OverrideMethodAdvice( in AdviceConstructorParameters<IMethod> parameters, BoundTemplateMethod boundTemplate )
        : base( parameters )
    {
        this._boundTemplate = boundTemplate;
    }

    public override AdviceKind AdviceKind
        => this.TargetDeclaration.MethodKind == MethodKind.Finalizer ? AdviceKind.OverrideFinalizer : AdviceKind.OverrideMethod;

    protected override OverrideMemberAdviceResult<IMethod> Implement( AdviceImplementationContext context )
    {
        // TODO: order should be self if the target is introduced on the same layer.
        var targetMethod = this.TargetDeclaration;

        switch ( targetMethod.MethodKind )
        {
            case MethodKind.Finalizer:
                context.AddTransformation(
                    new OverrideFinalizerTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._boundTemplate ) );

                break;

            default:
                // OverrideMethodTransformation handles both regular methods and operators.
                context.AddTransformation(
                    new OverrideMethodTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef(), this._boundTemplate ) );

                break;
        }

        // If the target method does not have an implementation (e.g. partial method without implementation part),
        // emit a transformation to update the HasImplementation flag in the code model.
        if ( !targetMethod.HasImplementation )
        {
            context.AddTransformation(
                new SetHasImplementationTransformation( this.AspectLayerInstance, this.TargetDeclaration.ToFullRef().As<IMember>() ) );
        }

        return this.CreateSuccessResult( targetMethod );
    }
}