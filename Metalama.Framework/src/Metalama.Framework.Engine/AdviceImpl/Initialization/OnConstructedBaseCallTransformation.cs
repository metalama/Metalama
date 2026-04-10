// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Aggregatable transformation that emits the <c>base.OnConstructed(context.Descend(slotA | slotB | ...))</c>
/// prologue statement at the top of an introduced <c>OnConstructed</c> override. When multiple aspects
/// (each with its own set of slot fields) target the same override method, the linker aggregates the
/// adjacent transformations and only the first one is asked to produce the statement — it then collects
/// the slot field lists from all peers (including itself) and combines them with the <c>|</c> operator
/// so a single <c>base.OnConstructed(context.Descend(...))</c> call covers all slots.
/// </summary>
internal sealed class OnConstructedBaseCallTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation, IAggregatableInsertStatementTransformation
{
    private readonly IFullRef<IMethod> _targetMethod;
    private readonly string _contextParameterName;
    private readonly IReadOnlyList<IField>? _slotFields;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetMethod;

    public string AggregateKey => "OnConstructed_Prologue_BaseCall";

    /// <summary>
    /// Slot fields contributed by this transformation. Exposed to peer transformations in an aggregated
    /// group so the first transformation can merge them into a single <c>|</c>-combined expression.
    /// </summary>
    public IReadOnlyList<IField>? SlotFields => this._slotFields;

    public OnConstructedBaseCallTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> contextDeclaration,
        IFullRef<IMethod> targetMethod,
        string contextParameterName,
        IReadOnlyList<IField>? slotFields ) : base( aspectLayerInstance, targetMethod )
    {
        this.ContextDeclaration = contextDeclaration;
        this._targetMethod = targetMethod;
        this._contextParameterName = contextParameterName;
        this._slotFields = slotFields;
    }

    public IReadOnlyList<InsertedStatement> GetInsertedStatements(
        InsertStatementTransformationContext context,
        IReadOnlyList<IInsertStatementTransformation>? aggregatedGroup = null )
    {
        // Collect slot fields: either just our own, or all peers' fields when aggregated.
        List<IField>? allSlotFields = null;

        void AddFields( IReadOnlyList<IField>? fields )
        {
            if ( fields == null || fields.Count == 0 )
            {
                return;
            }

            allSlotFields ??= new List<IField>();
            allSlotFields.AddRange( fields );
        }

        if ( aggregatedGroup != null )
        {
            foreach ( var peer in aggregatedGroup )
            {
                if ( peer is OnConstructedBaseCallTransformation peerTransformation )
                {
                    AddFields( peerTransformation._slotFields );
                }
            }
        }
        else
        {
            AddFields( this._slotFields );
        }

        var slotExpression = BuildSlotExpression( allSlotFields );
        var statement = BuildBaseOnConstructedCall( this._contextParameterName, slotExpression );

        return
        [
            new InsertedStatement(
                statement
                    .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                this.ContextDeclaration.GetTarget( context.FinalCompilation ),
                this,
                InsertedStatementKind.InitializerPrologue )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this._targetMethod;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add base.OnConstructed call prologue to '{this._targetMethod}'.";

    /// <summary>
    /// Builds the combination of slot fields using the <c>|</c> operator, or <c>null</c> when there are no slots.
    /// </summary>
    private static ExpressionSyntax? BuildSlotExpression( IReadOnlyList<IField>? slotFields )
    {
        if ( slotFields == null || slotFields.Count == 0 )
        {
            return null;
        }

        ExpressionSyntax? result = null;

        foreach ( var field in slotFields )
        {
            var fieldAccess = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactoryEx.CreateFullyQualifiedName( field.DeclaringType.FullName ),
                SyntaxFactoryEx.SafeIdentifierName( field.Name ) );

            result = result == null
                ? fieldAccess
                : BinaryExpression( SyntaxKind.BitwiseOrExpression, result, fieldAccess );
        }

        return result;
    }

    /// <summary>
    /// Builds <c>base.OnConstructed(context.Descend(slotExpression))</c> when <paramref name="slotExpression"/>
    /// is non-null, or <c>base.OnConstructed(context)</c> when it is null.
    /// </summary>
    private static StatementSyntax BuildBaseOnConstructedCall( string contextParameterName, ExpressionSyntax? slotExpression )
    {
        ExpressionSyntax argument = SyntaxFactoryEx.SafeIdentifierName( contextParameterName );

        if ( slotExpression != null )
        {
            argument = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    argument,
                    IdentifierName( "Descend" ) ),
                ArgumentList(
                    SingletonSeparatedList(
                        Argument( slotExpression ) ) ) );
        }

        return ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    BaseExpression(),
                    IdentifierName( "OnConstructed" ) ),
                ArgumentList(
                    SingletonSeparatedList( Argument( argument ) ) ) ) );
    }
}
