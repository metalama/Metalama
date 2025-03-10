// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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