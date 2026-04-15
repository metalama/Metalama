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
/// Abstract base class for aggregatable transformations that emit a <c>base.{method}(context.Descend(slotA | slotB | ...))</c>
/// prologue statement at the top of an introduced override. Shared between <see cref="OnConstructedBaseCallTransformation"/>
/// and <see cref="InitializeBaseCallTransformation"/>: the two only differ in the called method name and the aggregate key.
/// When multiple aspects (each with its own set of slot fields) target the same override method, the linker aggregates the
/// peer transformations and only the first one is asked to produce the statement — it then collects the slot field lists
/// from all peers (including itself) and combines them with the <c>|</c> operator so a single <c>base.{method}(context.Descend(...))</c>
/// call covers all slots.
/// </summary>
internal abstract class BaseInitializationCallTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation, IAggregatableInsertStatementTransformation
{
    private readonly IFullRef<IMethod> _targetMethod;
    private readonly string _contextParameterName;
    private readonly IReadOnlyList<IField>? _slotFields;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetMethod;

    /// <summary>
    /// Gets the name of the method called on <c>base</c> (e.g. <c>"OnConstructed"</c> or <c>"Initialize"</c>).
    /// </summary>
    protected abstract string MethodName { get; }

    public abstract object AggregateKey { get; }

    protected BaseInitializationCallTransformation(
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
        // The linker groups by AggregateKey, so every peer here is an instance of the same concrete subclass —
        // casting to the shared base is sufficient to access `_slotFields`.
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
                if ( peer is BaseInitializationCallTransformation peerTransformation )
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
        var statement = this.BuildBaseCall( this._contextParameterName, slotExpression );

        return
        [
            new InsertedStatement(
                statement
                    .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                this.ContextDeclaration.GetTarget( context.FinalCompilation ),
                this,
                InsertedStatementKind.InitializerBase )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this._targetMethod;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add base.{this.MethodName} call prologue to '{this._targetMethod}'.";

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
    /// Builds <c>base.{MethodName}(context.Descend(slotExpression))</c> when <paramref name="slotExpression"/>
    /// is non-null, or <c>base.{MethodName}(context)</c> when it is null.
    /// </summary>
    private StatementSyntax BuildBaseCall( string contextParameterName, ExpressionSyntax? slotExpression )
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
                    SyntaxFactoryEx.SafeIdentifierName( this.MethodName ) ),
                ArgumentList(
                    SingletonSeparatedList( Argument( argument ) ) ) ) );
    }
}
