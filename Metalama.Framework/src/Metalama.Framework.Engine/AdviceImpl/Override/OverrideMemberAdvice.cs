// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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