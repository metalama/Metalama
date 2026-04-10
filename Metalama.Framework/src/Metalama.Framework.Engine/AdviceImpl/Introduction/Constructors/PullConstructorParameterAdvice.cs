// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed class PullConstructorParameterAdvice : Advice<EmptyAdviceResult>
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly IParameter _parameter;
    private readonly IConstructorOverloadingStrategy? _overloadingStrategy;

    public PullConstructorParameterAdvice(
        in AdviceConstructorParameters parameters,
        IPullStrategy? pullStrategy,
        IParameter parameter,
        IConstructorOverloadingStrategy? overloadingStrategy = null ) : base( in parameters )
    {
        this._pullStrategy = pullStrategy;
        this._parameter = parameter;
        this._overloadingStrategy = overloadingStrategy;
    }

    protected override bool AcceptsExternalTargets => true;

    public override AdviceKind AdviceKind => AdviceKind.PullConstructorParameter;

    protected override EmptyAdviceResult Implement( AdviceImplementationContext context )
    {
        var forwardingHelper = this._overloadingStrategy is not null
            ? new ForwardingConstructorHelper( context, this.AspectLayerInstance, this._overloadingStrategy, this._pullStrategy, this )
            : null;

        var impl = new PullConstructorParameterAdviceImpl( context, this._pullStrategy, this.AspectLayerInstance, true, forwardingHelper );
        impl.PullConstructorParameterRecursive( this._parameter );

        return new EmptyAdviceResult( AdviceKind.PullConstructorParameter, AdviceOutcome.Success, this.AdviceFactory );
    }

    protected override EmptyAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceKind.PullConstructorParameter, AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}