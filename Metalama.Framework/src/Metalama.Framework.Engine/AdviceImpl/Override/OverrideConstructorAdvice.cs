// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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