// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Transformations;

/// <summary>
/// A transformation that inserts a syntax-based statement into a constructor or method body.
/// </summary>
internal sealed class InsertSyntaxStatementsTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation
{
    private readonly IFullRef<IMethodBase> _targetMethodBase;
    private readonly Func<SyntaxGenerationContext, StatementSyntax> _statement;
    private readonly InsertedStatementKind _statementKind;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetMethodBase;

    public InsertSyntaxStatementsTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> contextDeclaration,
        IFullRef<IMethodBase> targetMethodBase,
        Func<SyntaxGenerationContext, StatementSyntax> statement,
        InsertedStatementKind statementKind ) : base( aspectLayerInstance, targetMethodBase )
    {
        this.ContextDeclaration = contextDeclaration;
        this._targetMethodBase = targetMethodBase;
        this._statement = statement;
        this._statementKind = statementKind;
    }

    public IReadOnlyList<InsertedStatement> GetInsertedStatements(
        InsertStatementTransformationContext context,
        IReadOnlyList<IInsertStatementTransformation>? aggregatedGroup = null )
    {
        return
        [
            new InsertedStatement(
                this._statement( context.SyntaxGenerationContext )
                    .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                this.ContextDeclaration.GetTarget( context.FinalCompilation ),
                this,
                this._statementKind )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetMemberOrNamedType;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add a statement to '{this._targetMethodBase}'.";
}