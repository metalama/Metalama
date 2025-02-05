// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal abstract class OverrideMemberAdvice<TInput, TOutput> : Advice<OverrideMemberAdviceResult<TOutput>>
    where TInput : class, IMember
    where TOutput : class, IMember
{
    protected new TInput TargetDeclaration => (TInput) base.TargetDeclaration;

    protected OverrideMemberAdvice( AdviceConstructorParameters<TInput> parameters ) : base( parameters ) { }

    public override string ToString() => $"Override {this.TargetDeclaration}";

    protected OverrideMemberAdviceResult<TOutput> CreateSuccessResult( TOutput? member = null )
        => new( member?.ToRef().As<TOutput>() ) { AdviceKind = this.AdviceKind, Outcome = AdviceOutcome.Default };
}