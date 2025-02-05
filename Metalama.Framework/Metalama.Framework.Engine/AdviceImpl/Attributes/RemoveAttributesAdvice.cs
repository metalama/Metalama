// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class RemoveAttributesAdvice : Advice<RemoveAttributesAdviceResult>
{
    private readonly INamedType _attributeType;

    public RemoveAttributesAdvice( AdviceConstructorParameters parameters, INamedType attributeType ) : base( parameters )
    {
        this._attributeType = attributeType;
    }

    public override AdviceKind AdviceKind => AdviceKind.RemoveAttributes;

    protected override RemoveAttributesAdviceResult Implement( in AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration;

        if ( targetDeclaration.Attributes.OfAttributeType( this._attributeType ).Any() )
        {
            context.AddTransformation(
                new RemoveAttributesTransformation(
                    this.AspectLayerInstance,
                    targetDeclaration.ToFullRef(),
                    this._attributeType.ToFullRef() ) );
        }

        return new RemoveAttributesAdviceResult();
    }
}