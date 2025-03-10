// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class AddAttributeAdvice : Advice<AddAttributeAdviceResult>
{
    private readonly IAttributeData _attribute;
    private readonly OverrideStrategy _overrideStrategy;

    public AddAttributeAdvice( AdviceConstructorParameters parameters, IAttributeData attribute, OverrideStrategy overrideStrategy )
        : base( parameters )
    {
        this._attribute = attribute;
        this._overrideStrategy = overrideStrategy;
    }

    public override AdviceKind AdviceKind => AdviceKind.IntroduceAttribute;

    protected override AddAttributeAdviceResult Implement( in AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration;
        var contextCopy = context;

        if ( this._overrideStrategy != OverrideStrategy.New )
        {
            // Determine if we already have a custom attribute of this type, and handle conflict.

            var existingAttribute = targetDeclaration.Attributes.OfAttributeType( this._attribute.Type ).FirstOrDefault();

            if ( existingAttribute != null )
            {
                // There is a conflict.

                switch ( this._overrideStrategy )
                {
                    case OverrideStrategy.Fail:
                        return this.CreateFailedResult(
                            AdviceDiagnosticDescriptors.AttributeAlreadyPresent.CreateRoslynDiagnostic(
                                targetDeclaration.GetDiagnosticLocation(),
                                (this.AspectInstance.AspectClass.ShortName, this._attribute.Type, targetDeclaration),
                                this ) );

                    case OverrideStrategy.Ignore:
                        return new AddAttributeAdviceResult( AdviceOutcome.Ignore, existingAttribute.ToRef() );

                    case OverrideStrategy.Override:
                        var removeTransformation = new RemoveAttributesTransformation(
                            this.AspectLayerInstance,
                            targetDeclaration.ToFullRef(),
                            this._attribute.Type.ToFullRef() );

                        return AddTransformations( AdviceOutcome.Override, removeTransformation );

                    default:
                        throw new AssertionFailedException( $"Invalid value of OverrideStrategy: {this._overrideStrategy}." );
                }
            }
        }

        return AddTransformations( AdviceOutcome.Default );

        AddAttributeAdviceResult AddTransformations( AdviceOutcome outcome, RemoveAttributesTransformation? removeTransformation = null )
        {
            if ( removeTransformation != null )
            {
                contextCopy.AddTransformation( removeTransformation );
            }

            if ( targetDeclaration.ContainingDeclaration is IConstructor { IsImplicitlyDeclared: true } constructor )
            {
                contextCopy.AddTransformation(
                    new ConstructorBuilder( this.AspectLayerInstance, constructor )
                        .CreateTransformation() );
            }

            var attributeBuilder = new AttributeBuilder( this.AspectLayerInstance, targetDeclaration, this._attribute );
            attributeBuilder.Freeze();
            contextCopy.AddTransformation( attributeBuilder.CreateTransformation() );

            return new AddAttributeAdviceResult( outcome, attributeBuilder.ToRef() );
        }
    }
}