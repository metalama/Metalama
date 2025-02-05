// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

internal abstract class OverrideMemberTransformation : BaseSyntaxTreeTransformation, IInjectMemberTransformation, IOverrideDeclarationTransformation
{
    public abstract IFullRef<IMember> OverriddenDeclaration { get; }

    IFullRef<IDeclaration> IOverrideDeclarationTransformation.OverriddenDeclaration => this.OverriddenDeclaration;

    public override IFullRef<IDeclaration> TargetDeclaration => this.OverriddenDeclaration;

    protected OverrideMemberTransformation( AspectLayerInstance aspectLayerInstance, IFullRef<IDeclaration> overriddenDeclaration ) : base(
        aspectLayerInstance,
        overriddenDeclaration )
    {
        Invariant.Assert( aspectLayerInstance != null! );
    }

    public abstract IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context );

    protected ExpressionSyntax CreateMemberAccessExpression( AspectReferenceTargetKind referenceTargetKind, MemberInjectionContext context )
        => ProceedHelper.CreateMemberAccessExpression(
            this.OverriddenDeclaration.GetTarget( this.InitialCompilation ),
            this.AspectLayerId,
            referenceTargetKind,
            context.SyntaxGenerationContext );

    public InsertPosition InsertPosition => this.OverriddenDeclaration.ToInsertPosition();

    public override TransformationObservability Observability => TransformationObservability.None;

    public override FormattableString ToDisplayString()
        => $"Override the {this.OverriddenDeclaration.DeclarationKind} '{this.OverriddenDeclaration.Definition.ToDisplayString()}'";

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.OverrideMember;

    public override string ToString() => $"{{{this.GetType().Name} OverriddenDeclaration={{{this.OverriddenDeclaration}}}";
}