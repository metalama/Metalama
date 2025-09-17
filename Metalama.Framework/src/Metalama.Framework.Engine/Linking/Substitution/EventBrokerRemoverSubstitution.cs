// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes event broker remover body for event accessors.
/// </summary>
internal sealed class EventBrokerRemoverSubstitution : SyntaxNodeSubstitution
{
    private readonly ResolvedAspectReference _aspectReference;

    public EventBrokerRemoverSubstitution(
        CompilationContext compilationContext,
        ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        this._aspectReference = aspectReference;
    }

    public override SyntaxNode ReplacedNode => this._aspectReference.RootNode;

    public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        var context = substitutionContext.SyntaxGenerationContext;
        var eventOverride = this._aspectReference.ResolvedSemantic.Symbol;
        var @event = (IEventSymbol) substitutionContext.RewritingDriver.InjectionRegistry.GetOverrideTarget( eventOverride ).AssertNotNull();
        var eventOverrideTransformation = substitutionContext.RewritingDriver.InjectionRegistry.GetTransformationForSymbol( eventOverride ).AssertNotNull();
        var eventBrokerTypeInfo = substitutionContext.RewritingDriver.AnalysisRegistry.GetEventBrokerTypeInfo( @event ).AssertNotNull();
        var eventBrokerTransformationInfo = eventBrokerTypeInfo.Transformations[eventOverrideTransformation];

        return context.SyntaxGenerator.FormattedBlock(
            ExpressionStatement(
                ConditionalAccessExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName( eventBrokerTransformationInfo.EventBrokerFieldName ) ),
                    InvocationExpression(
                        MemberBindingExpression(
                            IdentifierName( "RemoveHandler" ) ),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName( "value" ) ) ) ) ) ) ) );
    }
}