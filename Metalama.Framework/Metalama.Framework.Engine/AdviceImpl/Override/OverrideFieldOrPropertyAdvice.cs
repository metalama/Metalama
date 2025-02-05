// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideFieldOrPropertyAdvice : OverrideMemberAdvice<IFieldOrProperty, IProperty>
{
    private readonly BoundTemplateMethod? _getTemplate;
    private readonly BoundTemplateMethod? _setTemplate;

    public OverrideFieldOrPropertyAdvice(
        AdviceConstructorParameters<IFieldOrProperty> parameters,
        BoundTemplateMethod? getTemplate,
        BoundTemplateMethod? setTemplate )
        : base( parameters )
    {
        this._getTemplate = getTemplate.ExplicitlyImplementedOrNull();
        this._setTemplate = setTemplate.ExplicitlyImplementedOrNull();
    }

    public override AdviceKind AdviceKind => AdviceKind.OverrideFieldOrPropertyOrIndexer;

    protected override OverrideMemberAdviceResult<IProperty> Implement( in AdviceImplementationContext context )
    {
        // TODO: Translate templates to this compilation.
        // TODO: order should be self if the target is introduced on the same layer.
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        var promotedField = OverrideHelper.OverrideProperty(
            context.ServiceProvider,
            this.AspectLayerInstance,
            targetDeclaration,
            this._getTemplate,
            this._setTemplate,
            context.AddTransformation );

        return this.CreateSuccessResult( promotedField );
    }
}