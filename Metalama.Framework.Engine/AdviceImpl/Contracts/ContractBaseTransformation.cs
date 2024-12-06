// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal abstract class ContractBaseTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation
{
    private readonly TemplateMember<IMethod> _template;
    private readonly IObjectReader _templateArguments;
    
    /// <summary>
    /// Gets the target member of the contract into which contract statements will be inserted.
    /// </summary>
    public abstract IFullRef<IMember> TargetMember { get; }

    /// <summary>
    /// Gets the declaration on which the contract was applied on.
    /// </summary>
    protected IRef<IDeclaration> ContractTarget { get; }

    /// <summary>
    /// Gets the contract direction of inserted statements.
    /// </summary>
    protected ContractDirection ContractDirection { get; }

    protected ContractBaseTransformation(
        AspectLayerInstance aspectLayerInstance,
        IFullRef<IDeclaration> contractTarget,
        ContractDirection contractDirection,
        TemplateMember<IMethod> template,
        IObjectReader templateArguments ) : base( aspectLayerInstance, contractTarget )
    {
        Invariant.Assert( contractDirection is not ContractDirection.None );

        this.ContractTarget = contractTarget;
        this.ContractDirection = contractDirection;
        this._template = template;
        this._templateArguments = templateArguments;
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMember;

    public abstract IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context );

    protected bool TryExecuteTemplate(
        TransformationContext context,
        ExpressionSyntax valueExpression,
        IType valueType,
        [NotNullWhen( true )] out BlockSyntax? contractBlock )
    {
        var annotatedValueExpression = TypeAnnotationMapper.AddExpressionTypeAnnotation( valueExpression, valueType );
        var boundTemplate = this._template.ForContract( annotatedValueExpression, this._templateArguments );

        var metaApiProperties = new MetaApiProperties(
            this.InitialCompilation,
            context.DiagnosticSink,
            this._template.AsMemberOrNamedType(),
            this.AspectLayerId,
            context.SyntaxGenerationContext,
            this.AspectInstance,
            context.ServiceProvider,
            MetaApiStaticity.Default );

        var metaApi = MetaApi.ForDeclaration(
            this.ContractTarget.GetTarget( context.FinalCompilation ),
            metaApiProperties,
            this.ContractDirection );

        var expansionContext = new TemplateExpansionContext(
            context,
            metaApi,
            this.TargetMember.GetTarget( context.FinalCompilation ),
            boundTemplate,
            null,
            this.AspectLayerId );

        var templateDriver = this._template.Driver;

        return templateDriver.TryExpandDeclaration( expansionContext, boundTemplate.TemplateArguments, out contractBlock );
    }

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override string ToString() => $"{this.GetType().Name} ContractTarget={{{this.ContractTarget}}}";

    public override FormattableString ToDisplayString()
    {
        var parameter = this.ContractTarget.GetTarget( this.InitialCompilation ) switch
        {
            IParameter { IsReturnParameter: true } => "return value",
            IParameter param => $"parameter '{param.Name}'",
            var target => $"unexpected declaration '{target}'"
        };

        return $"Add contract to {parameter} of {this.TargetMember.DeclarationKind} '{this.TargetMember}'";
    }
}