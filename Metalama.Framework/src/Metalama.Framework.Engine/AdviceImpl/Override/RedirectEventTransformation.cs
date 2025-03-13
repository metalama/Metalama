// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// Represents an event override, which redirects to accessors of another event without requiring template expansion.
/// </summary>
internal sealed class RedirectEventTransformation : OverrideMemberTransformation
{
    private readonly IFullRef<IEvent> _overriddenDeclaration;
    private readonly IFullRef<IEvent> _targetEvent;

    public RedirectEventTransformation( AspectLayerInstance aspectLayerInstance, IFullRef<IEvent> overriddenDeclaration, IFullRef<IEvent> targetEvent )
        : base( aspectLayerInstance, overriddenDeclaration )
    {
        this._overriddenDeclaration = overriddenDeclaration;
        this._targetEvent = targetEvent;
    }

    public override IFullRef<IMember> OverriddenDeclaration => this._overriddenDeclaration;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        var overriddenDeclaration = this._overriddenDeclaration.GetTarget( this.InitialCompilation );

        return
        [
            new InjectedMember(
                this,
                EventDeclaration(
                    List<AttributeListSyntax>(),
                    overriddenDeclaration.GetSyntaxModifierList(),
                    context.SyntaxGenerator.EventType( overriddenDeclaration ),
                    null,
                    Identifier(
                        context.InjectionNameProvider.GetOverrideName(
                            overriddenDeclaration.DeclaringType,
                            this.AspectLayerId,
                            overriddenDeclaration ) ),
                    AccessorList( List( GetAccessors() ) ) ),
                this.AspectLayerId,
                InjectedMemberSemantic.Override,
                overriddenDeclaration.ToFullRef() )
        ];

        IEnumerable<AccessorDeclarationSyntax> GetAccessors()
        {
            return new[]
            {
                AccessorDeclaration(
                    SyntaxKind.AddAccessorDeclaration,
                    List<AttributeListSyntax>(),
                    overriddenDeclaration.AddMethod.GetSyntaxModifierList(),
                    CreateAccessorBody( SyntaxKind.AddAssignmentExpression ),
                    null ),
                AccessorDeclaration(
                    SyntaxKind.RemoveAccessorDeclaration,
                    List<AttributeListSyntax>(),
                    overriddenDeclaration.RemoveMethod.GetSyntaxModifierList(),
                    CreateAccessorBody( SyntaxKind.SubtractAssignmentExpression ),
                    null )
            }.WhereNotNull();
        }

        BlockSyntax CreateAccessorBody( SyntaxKind assignmentKind )
        {
            return
                context.SyntaxGenerationContext.SyntaxGenerator.FormattedBlock(
                    ExpressionStatement(
                        AssignmentExpression(
                            assignmentKind,
                            CreateAccessTargetExpression(),
                            IdentifierName( "value" ) ) ) );
        }

        ExpressionSyntax CreateAccessTargetExpression()
        {
            return
                this._targetEvent.Definition.IsStatic
                    ? IdentifierName( this._targetEvent.Name.AssertNotNull() )
                    : MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            IdentifierName( this._targetEvent.Name.AssertNotNull() ) )
                        .WithAspectReferenceAnnotation( this.AspectLayerId, AspectReferenceOrder.Previous );
        }
    }
}