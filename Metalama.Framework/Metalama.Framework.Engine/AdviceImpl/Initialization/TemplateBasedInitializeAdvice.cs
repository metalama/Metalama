// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class TemplateBasedInitializeAdvice : InitializeAdvice
{
    private readonly BoundTemplateMethod _boundTemplate;

    public TemplateBasedInitializeAdvice(
        AdviceConstructorParameters<IMemberOrNamedType> parameters,
        BoundTemplateMethod boundTemplate,
        InitializerKind kind )
        : base( parameters, kind )
    {
        this._boundTemplate = boundTemplate;
    }

    protected override void AddTransformation( IMemberOrNamedType targetDeclaration, IConstructor targetCtor, Action<ITransformation> addTransformation )
    {
        var initialization = new TemplateBasedInitializationTransformation(
            this.AspectLayerInstance,
            targetDeclaration.ToRef(),
            targetCtor.ToFullRef(),
            this._boundTemplate );

        addTransformation( initialization );
    }
}