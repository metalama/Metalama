// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class TemplateBasedConstructorInitializeAdvice : ConstructorInitializeAdvice
{
    private readonly TemplateMember<IMethod> _template;
    private readonly IObjectReader? _templateArguments;

    public TemplateBasedConstructorInitializeAdvice(
        in AdviceConstructorParameters<IMemberOrNamedType> parameters,
        TemplateMember<IMethod> template,
        IObjectReader? templateArguments,
        InitializerKind kind )
        : base( parameters, kind )
    {
        this._template = template;
        this._templateArguments = templateArguments;
    }

    protected override void AddTransformation( IMemberOrNamedType targetDeclaration, IConstructor targetCtor, Action<ITransformation> addTransformation )
    {
        // Bind the template per-constructor so that any run-time InitializationContext parameter
        // can be mapped to the target constructor's actual context parameter name.
        var boundTemplate = this._template.ForInitializer( targetCtor, this._templateArguments );

        var initialization = new InsertTemplateStatementsTransformation(
            this.AspectLayerInstance,
            targetDeclaration.ToRef(),
            targetCtor.ToFullRef(),
            boundTemplate,
            InsertedStatementKind.InitializerAfterBase );

        addTransformation( initialization );
    }
}