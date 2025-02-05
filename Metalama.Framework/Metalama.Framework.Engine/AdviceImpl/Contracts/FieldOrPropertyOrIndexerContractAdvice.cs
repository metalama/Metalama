// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal sealed class FieldOrPropertyOrIndexerContractAdvice : ContractAdvice<IFieldOrPropertyOrIndexer>
{
    public FieldOrPropertyOrIndexerContractAdvice(
        AdviceConstructorParameters<IFieldOrPropertyOrIndexer> parameters,
        TemplateMember<IMethod> template,
        ContractDirection direction,
        IObjectReader templateArguments )
        : base( parameters, template, direction, templateArguments ) { }

    protected override AddContractAdviceResult<IFieldOrPropertyOrIndexer> Implement( in AdviceImplementationContext context )
    {
        var serviceProvider = context.ServiceProvider;
        var contextCopy = context;
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        switch ( targetDeclaration )
        {
            case IProperty property:
                return AddContractToProperty( property );

            case IField { OverridingProperty: { } overridingProperty }:
                return AddContractToProperty( overridingProperty );

            case IField field:
                var transformation = PromoteFieldTransformation.Create( serviceProvider, field, this.AspectLayerInstance );
                context.AddTransformation( transformation );
                OverrideHelper.AddTransformationsForStructField( field.DeclaringType, this.AspectLayerInstance, context.AddTransformation );

                return AddContractToProperty( transformation.OverridingProperty );

            case IIndexer indexer:
                context.AddTransformation(
                    new ContractIndexerTransformation(
                        this.AspectLayerInstance,
                        indexer.ToFullRef(),
                        null,
                        this.Direction,
                        this.Template,
                        this.TemplateArguments ) );

                return CreateSuccessResult( indexer );

            default:
                throw new AssertionFailedException();
        }

        AddContractAdviceResult<IFieldOrPropertyOrIndexer> AddContractToProperty( IProperty property )
        {
            contextCopy.AddTransformation(
                new ContractPropertyTransformation(
                    this.AspectLayerInstance,
                    property.ToFullRef(),
                    this.Direction,
                    this.Template,
                    this.TemplateArguments ) );

            return CreateSuccessResult( property );
        }
    }
}