// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.LinkerTests.Runner;

/// <summary>
/// Represents a test transformation that takes syntax of a PseudoIntroduction-marked member and injects it.
/// </summary>
internal class TestIntroduceDeclarationTransformation : TestTransformationBase, IIntroduceDeclarationTransformation
{
    private readonly MemberDeclarationSyntax _syntax;

    public DeclarationBuilderData DeclarationBuilderData { get; }

    public TestIntroduceDeclarationTransformation(
        AspectLayerInstance aspectLayerInstance, 
        InsertPosition insertPosition, 
        DeclarationBuilderData declarationBuilderData,
        MemberDeclarationSyntax syntax )
        : base( aspectLayerInstance, insertPosition )
    {
        this.DeclarationBuilderData = declarationBuilderData;
        this._syntax = syntax;
    }

    public override TransformationObservability Observability => TransformationObservability.Always;

    public override SyntaxTree TransformedSyntaxTree => this.DeclarationBuilderData.ContainingDeclaration.GetPrimaryDeclarationSyntax().SyntaxTree;

    public override IRef<IDeclaration> TargetDeclaration => this.DeclarationBuilderData.ToFullRef();

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        yield return new InjectedMember( this, this._syntax, this.AspectLayerId, InjectedMemberSemantic.Introduction, this.DeclarationBuilderData.ContainingDeclaration );
    }
}
