// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal abstract class ContractAdvice<T> : Advice<AddContractAdviceResult<T>>
    where T : class, IDeclaration
{
    protected ContractDirection Direction { get; }

    protected TemplateMember<IMethod> Template { get; }

    protected IObjectReader TemplateArguments { get; }

    protected ContractAdvice(
        AdviceConstructorParameters<T> parameters,
        TemplateMember<IMethod> template,
        ContractDirection direction,
        IObjectReader templateArguments )
        : base( parameters )
    {
        Invariant.Assert( direction is ContractDirection.Input or ContractDirection.Output or ContractDirection.Both );

        this.Direction = direction;
        this.Template = template;
        this.TemplateArguments = templateArguments;
    }

    public override AdviceKind AdviceKind => AdviceKind.AddContract;

    // TODO: the conversion on the next line will not work with fields.
    protected static AddContractAdviceResult<T> CreateSuccessResult( T member ) => new( member.ToRef().As<T>() );
}