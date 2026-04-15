// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Introspection;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// A transformation that expands a template and inserts the resulting statement into a constructor or method body.
/// </summary>
internal sealed class InsertTemplateStatementsTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation
{
    private readonly IFullRef<IMethodBase> _targetMethodBase;
    private readonly BoundTemplateMethod _boundTemplate;
    private readonly InsertedStatementKind _statementKind;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetMethodBase;

    public InsertTemplateStatementsTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> contextDeclaration,
        IFullRef<IMethodBase> targetMethodBase,
        BoundTemplateMethod boundTemplate,
        InsertedStatementKind statementKind ) : base( aspectLayerInstance, targetMethodBase )
    {
        this.ContextDeclaration = contextDeclaration;
        this._targetMethodBase = targetMethodBase;
        this._boundTemplate = boundTemplate;
        this._statementKind = statementKind;
    }

    public IReadOnlyList<InsertedStatement> GetInsertedStatements(
        InsertStatementTransformationContext context,
        IReadOnlyList<IInsertStatementTransformation>? aggregatedGroup = null )
    {
        var target = this._targetMethodBase.GetTarget( this.InitialCompilation );
        var contextDeclaration = this.ContextDeclaration.GetTarget( this.InitialCompilation );

        var metaApiProperties = new MetaApiProperties(
            this.InitialCompilation,
            context.DiagnosticSink,
            this._boundTemplate.TemplateMember.AsMemberOrNamedType(),
            this.AspectLayerId,
            context.SyntaxGenerationContext,
            this.AspectInstance,
            context.ServiceProvider,
            AdviceKind.AddInitializer );

        var metaApi = target.DeclarationKind switch
        {
            DeclarationKind.Constructor => MetaApi.ForConstructor( (IConstructor) target, metaApiProperties ),
            DeclarationKind.Method => MetaApi.ForMethod( (IMethod) target, metaApiProperties ),
            _ => throw new AssertionFailedException( $"Unexpected target declaration kind: {target.DeclarationKind}." )
        };

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
                this._statementKind )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMemberOrNamedType;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add a statement to '{this._targetMethodBase}'.";
}
