// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal sealed class ParameterContractAdvice : ContractAdvice<IParameter>
{
    public ParameterContractAdvice(
        AdviceConstructorParameters<IParameter> parameters,
        TemplateMember<IMethod> template,
        ContractDirection direction,
        IObjectReader templateArguments )
        : base( parameters, template, direction, templateArguments ) { }

    protected override AddContractAdviceResult<IParameter> Implement( in AdviceImplementationContext context )
    {
        var targetDeclaration = this.TargetDeclaration;

        switch ( targetDeclaration )
        {
            case IParameter { ContainingDeclaration: IIndexer indexer } parameter:
                context.AddTransformation(
                    new ContractIndexerTransformation(
                        this.AspectLayerInstance,
                        indexer.ToFullRef(),
                        parameter.ToFullRef(),
                        this.Direction,
                        this.Template,
                        this.TemplateArguments ) );

                return CreateSuccessResult( parameter );

            case IParameter { ContainingDeclaration: IMethod method } parameter:
                context.AddTransformation(
                    new ContractMethodTransformation(
                        this.AspectLayerInstance,
                        method.ToFullRef(),
                        parameter.ToFullRef(),
                        this.Direction,
                        this.Template,
                        this.TemplateArguments ) );

                return CreateSuccessResult( parameter );

            case IParameter { ContainingDeclaration: IConstructor constructor } parameter:
                context.AddTransformation(
                    new ContractConstructorTransformation(
                        this.AspectLayerInstance,
                        constructor.ToFullRef(),
                        parameter.ToFullRef(),
                        this.Direction,
                        this.Template,
                        this.TemplateArguments ) );

                return CreateSuccessResult( parameter );

            default:
                throw new AssertionFailedException();
        }
    }
}