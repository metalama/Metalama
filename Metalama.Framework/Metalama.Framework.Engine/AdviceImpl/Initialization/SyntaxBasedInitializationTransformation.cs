// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class SyntaxBasedInitializationTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation
{
    private readonly IFullRef<IConstructor> _targetConstructor;
    private readonly Func<SyntaxGenerationContext, StatementSyntax> _initializationStatement;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMember> TargetMember => this._targetConstructor;

    public SyntaxBasedInitializationTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> initializedDeclaration,
        IFullRef<IConstructor> targetConstructor,
        Func<SyntaxGenerationContext, StatementSyntax> initializationStatement ) : base( aspectLayerInstance, targetConstructor )
    {
        this.ContextDeclaration = initializedDeclaration;
        this._targetConstructor = targetConstructor;
        this._initializationStatement = initializationStatement;
    }

    public IReadOnlyList<InsertedStatement> GetInsertedStatements( InsertStatementTransformationContext context )
    {
        return
        [
            new InsertedStatement(
                this._initializationStatement( context.SyntaxGenerationContext )
                    .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                this.ContextDeclaration.GetTarget( context.FinalCompilation ),
                this,
                InsertedStatementKind.Initializer )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMember;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add a statement to '{this._targetConstructor}'.";
}