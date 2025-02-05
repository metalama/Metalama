// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal sealed class OverrideConstructorAdvice : OverrideMemberAdvice<IConstructor, IConstructor>
{
    private readonly BoundTemplateMethod _boundTemplate;

    public OverrideConstructorAdvice( AdviceConstructorParameters<IConstructor> parameters, BoundTemplateMethod boundTemplate )
        : base( parameters )
    {
        this._boundTemplate = boundTemplate;
    }

    public override AdviceKind AdviceKind => AdviceKind.OverrideConstructor;

    protected override OverrideMemberAdviceResult<IConstructor> Implement( in AdviceImplementationContext context )
    {
        var constructor = this.TargetDeclaration;

        if ( constructor.IsImplicitInstanceConstructor() )
        {
            // Missing implicit ctor.
            var builder = new ConstructorBuilder( this.AspectLayerInstance, constructor );

            builder.Freeze();

            context.AddTransformation( builder.CreateTransformation() );
            constructor = builder;
        }

        context.AddTransformation( new OverrideConstructorTransformation( this.AspectLayerInstance, constructor.ToFullRef(), this._boundTemplate ) );

        return this.CreateSuccessResult( constructor );
    }
}