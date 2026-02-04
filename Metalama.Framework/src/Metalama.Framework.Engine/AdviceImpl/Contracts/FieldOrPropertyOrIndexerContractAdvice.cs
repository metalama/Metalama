// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
        in AdviceConstructorParameters<IFieldOrPropertyOrIndexer> parameters,
        TemplateMember<IMethod> template,
        ContractDirection direction,
        IObjectReader templateArguments )
        : base( parameters, template, direction, templateArguments ) { }

    protected override AddContractAdviceResult<IFieldOrPropertyOrIndexer> Implement( AdviceImplementationContext context )
    {
        var serviceProvider = context.ServiceProvider;
        var contextCopy = context;
        var targetDeclaration = this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        switch ( targetDeclaration.DeclarationKind )
        {
            case DeclarationKind.Property when targetDeclaration is IProperty property:
                return AddContractToProperty( property );

            case DeclarationKind.Field when targetDeclaration is IField { OverridingProperty: { } overridingProperty }:
                return AddContractToProperty( overridingProperty );

            case DeclarationKind.Field when targetDeclaration is IField field:
                var transformation = PromoteFieldTransformation.Create( serviceProvider, field, this.AspectLayerInstance );
                context.AddTransformation( transformation );
                OverrideHelper.AddTransformationsForStructField( field.DeclaringType, this.AspectLayerInstance, context.AddTransformation );

                return AddContractToProperty( transformation.OverridingProperty );

            case DeclarationKind.Indexer when targetDeclaration is IIndexer indexer:
                context.AddTransformation(
                    new ContractIndexerTransformation(
                        this.AspectLayerInstance,
                        indexer.ToFullRef(),
                        null,
                        this.Direction,
                        this.Template,
                        this.TemplateArguments ) );

                return this.CreateSuccessResult( indexer );

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

            return this.CreateSuccessResult( property );
        }
    }
}