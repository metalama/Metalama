// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class TemplateBasedInitializationTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation
{
    private readonly IFullRef<IConstructor> _targetConstructor;
    private readonly BoundTemplateMethod _boundTemplate;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMember> TargetMember => this._targetConstructor;

    public TemplateBasedInitializationTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> initializedDeclaration,
        IFullRef<IConstructor> targetConstructor,
        BoundTemplateMethod boundTemplate ) : base( aspectLayerInstance, targetConstructor )
    {
        this.ContextDeclaration = initializedDeclaration;
        this._targetConstructor = targetConstructor;
        this._boundTemplate = boundTemplate;
    }

    public IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        var targetConstructor = this._targetConstructor.GetTarget( this.InitialCompilation );
        var contextDeclaration = this.ContextDeclaration.GetTarget( this.InitialCompilation );

        var metaApi = MetaApi.ForConstructor(
            targetConstructor,
            new MetaApiProperties(
                this.InitialCompilation,
                context.DiagnosticSink,
                this._boundTemplate.TemplateMember.AsMemberOrNamedType(),
                this.AspectLayerId,
                context.SyntaxGenerationContext,
                this.AspectInstance,
                context.ServiceProvider,
                targetConstructor.IsStatic ? MetaApiStaticity.AlwaysStatic : MetaApiStaticity.AlwaysInstance ) );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            contextDeclaration,
            this._boundTemplate,
            null,
            this.AspectLayerId );

        var templateDriver = this._boundTemplate.TemplateMember.Driver;

        if ( !templateDriver.TryExpandDeclaration( expansionContext, this._boundTemplate.TemplateArguments, out var expandedBody ) )
        {
            // Template expansion error.
            return Array.Empty<InsertedStatement>();
        }

        return
        [
            new InsertedStatement(
                expandedBody
                    .AssertNotNull()
                    .WithGeneratedCodeAnnotation(
                        metaApi.AspectInstance?.AspectClass.GeneratedCodeAnnotation ?? FormattingAnnotations.SystemGeneratedCodeAnnotation )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                contextDeclaration,
                this,
                InsertedStatementKind.Initializer )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMember;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add a statement to '{this._targetConstructor}'.";
}