// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Diagnostics;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal abstract class IntroduceDeclarationAdvice<TIntroduced, TBuilder> : Advice<IntroductionAdviceResult<TIntroduced>>
    where TIntroduced : class, IDeclaration
    where TBuilder : DeclarationBuilder, TIntroduced
{
    private readonly Action<TBuilder>? _buildAction;

    protected IAdviceFactoryImpl AdviceFactory { get; }

    protected IntroduceDeclarationAdvice( AdviceConstructorParameters parameters, Action<TBuilder>? buildAction, IAdviceFactoryImpl adviceFactory )
        : base( parameters )
    {
        this._buildAction = buildAction;
        this.AdviceFactory = adviceFactory;
    }

    protected IntroductionAdviceResult<TIntroduced> CreateSuccessResult( AdviceOutcome outcome, TIntroduced introducedMember )
    {
        return new IntroductionAdviceResult<TIntroduced>( this.AdviceKind, outcome, introducedMember.ToRef().As<TIntroduced>(), null, this.AdviceFactory );
    }

    protected IntroductionAdviceResult<TIntroduced> CreateIgnoredResult( IMemberOrNamedType existingMember )
        => new(
            this.AdviceKind,
            AdviceOutcome.Ignore,
            existingMember is TIntroduced typedMember ? typedMember.ToRef().As<TIntroduced>() : null,
            existingMember.ToRef(),
            this.AdviceFactory );

    protected sealed override IntroductionAdviceResult<TIntroduced> Implement( in AdviceImplementationContext context )
    {
        var builder = this.CreateBuilder();
        context.ThrowIfAnyError();

        this.InitializeBuilder( builder, in context );

        this._buildAction?.Invoke( builder );

        this.CompleteBuilder( builder );

        this.ValidateBuilder( builder, context.Diagnostics );

        return this.ImplementCore( builder, in context );
    }

    protected abstract TBuilder CreateBuilder();

    protected virtual void InitializeBuilder( TBuilder builder, in AdviceImplementationContext context ) { }

    protected virtual void CompleteBuilder( TBuilder builder ) { }

    protected abstract IntroductionAdviceResult<TIntroduced> ImplementCore( TBuilder builder, in AdviceImplementationContext context );

    protected virtual void ValidateBuilder( TBuilder builder, IDiagnosticAdder diagnosticAdder ) { }

    public override string ToString() => $"Introduce {typeof(TIntroduced).Name}";
}