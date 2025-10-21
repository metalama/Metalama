// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;

internal sealed class PullConstructorParameterAdvice : Advice<EmptyAdviceResult>
{
    private readonly IPullStrategy? _pullStrategy;
    private readonly IParameter _parameter;

    public PullConstructorParameterAdvice(
        in AdviceConstructorParameters parameters,
        IPullStrategy? pullStrategy,
        IParameter parameter ) : base( in parameters )
    {
        this._pullStrategy = pullStrategy;
        this._parameter = parameter;
    }

    protected override bool AcceptsExternalTargets => true;

    public override AdviceKind AdviceKind => AdviceKind.PullConstructorParameter;

    protected override EmptyAdviceResult Implement( AdviceImplementationContext context )
    {
        var impl = new PullConstructorParameterAdviceImpl( context, this._pullStrategy, this.AspectLayerInstance );
        impl.PullConstructorParameterRecursive( this._parameter );

        return EmptyAdviceResult.Instance;
    }
}