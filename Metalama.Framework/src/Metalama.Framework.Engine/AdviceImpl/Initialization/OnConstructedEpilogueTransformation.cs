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
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Aggregatable transformation that emits the constructor-epilogue call
/// <c>if (!context.IsHandled(InitializationSlot.OnConstructed)) { this.OnConstructed(context); }</c>
/// (or the unguarded form on sealed types / structs). When multiple aspects target the same constructor
/// they each emit this transformation; the linker aggregates the adjacent peers and only the first one is
/// asked to produce the statement, which deduplicates the otherwise-identical epilogue calls so the
/// constructor ends up with a single <c>OnConstructed</c> invocation.
/// </summary>
internal sealed class OnConstructedEpilogueTransformation : BaseSyntaxTreeTransformation, IInsertStatementTransformation,
                                                            IAggregatableInsertStatementTransformation
{
    private readonly IFullRef<IConstructor> _targetConstructor;
    private readonly string _contextParameterName;
    private readonly bool _guarded;

    private IRef<IMemberOrNamedType> ContextDeclaration { get; }

    public IFullRef<IMemberOrNamedType> TargetMemberOrNamedType => this._targetConstructor;

    private static readonly object _aggregateKey = new();

    public object AggregateKey => _aggregateKey;

    public OnConstructedEpilogueTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> contextDeclaration,
        IFullRef<IConstructor> targetConstructor,
        string contextParameterName,
        bool guarded ) : base( aspectLayerInstance, targetConstructor )
    {
        this.ContextDeclaration = contextDeclaration;
        this._targetConstructor = targetConstructor;
        this._contextParameterName = contextParameterName;
        this._guarded = guarded;
    }

    public IReadOnlyList<InsertedStatement> GetInsertedStatements(
        InsertStatementTransformationContext context,
        IReadOnlyList<IInsertStatementTransformation>? aggregatedGroup = null )
    {
        // All peers emit the same epilogue call (same target constructor, same context parameter, same guard
        // policy — which derives from the target type's sealed/struct status). Produce the statement once.
        var statement = BuildOnConstructedCallStatement( this._contextParameterName, this._guarded );

        return
        [
            new InsertedStatement(
                statement
                    .WithGeneratedCodeAnnotation( this.AspectInstance.AspectClass.GeneratedCodeAnnotation )
                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                this.ContextDeclaration.GetTarget( context.FinalCompilation ),
                this,
                InsertedStatementKind.InitializerEpilogue )
        ];
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this._targetConstructor;

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.InsertStatement;

    public override FormattableString ToDisplayString() => $"Add OnConstructed epilogue call to '{this._targetConstructor}'.";

    /// <summary>
    /// Builds the constructor-epilogue call. When <paramref name="guarded"/> is <c>true</c>, the call is
    /// wrapped in <c>if (!context.IsHandled(InitializationSlot.OnConstructed)) { ... }</c>.
    /// </summary>
    private static StatementSyntax BuildOnConstructedCallStatement( string contextParameterName, bool guarded )
    {
        var call = ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression(),
                    IdentifierName( "OnConstructed" ) ),
                ArgumentList( SingletonSeparatedList( Argument( SyntaxFactoryEx.SafeIdentifierName( contextParameterName ) ) ) ) ) );

        if ( !guarded )
        {
            return call;
        }

        var guardCondition = PrefixUnaryExpression(
            SyntaxKind.LogicalNotExpression,
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactoryEx.SafeIdentifierName( contextParameterName ),
                    IdentifierName( nameof(InitializationContext.IsHandled) ) ),
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactoryEx.CreateFullyQualifiedName( typeof(InitializationSlot).FullName! ),
                                IdentifierName( nameof(InitializationSlot.OnConstructed) ) ) ) ) ) ) );

        return IfStatement( guardCondition, Block( call ) );
    }
}