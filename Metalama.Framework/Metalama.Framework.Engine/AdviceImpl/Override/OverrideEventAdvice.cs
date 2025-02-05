// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideEventAdvice : OverrideMemberAdvice<IEvent, IEvent>
{
    private readonly BoundTemplateMethod? _addTemplate;
    private readonly BoundTemplateMethod? _removeTemplate;

    public OverrideEventAdvice(
        AdviceConstructorParameters<IEvent> parameters,
        BoundTemplateMethod? addTemplate,
        BoundTemplateMethod? removeTemplate )
        : base( parameters )
    {
        Invariant.Assert( addTemplate != null || removeTemplate != null );

        this._addTemplate = addTemplate;
        this._removeTemplate = removeTemplate;
    }

    public override AdviceKind AdviceKind => AdviceKind.OverrideEvent;

    protected override OverrideMemberAdviceResult<IEvent> Implement( in AdviceImplementationContext context )
    {
        // TODO: order should be self if the target is introduced on the same layer.
        context.AddTransformation(
            new OverrideEventTransformation(
                this.AspectLayerInstance,
                this.TargetDeclaration.ToFullRef(),
                this._addTemplate,
                this._removeTemplate ) );

        return this.CreateSuccessResult();
    }
}