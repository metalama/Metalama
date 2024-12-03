// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.LinkerTests.Runner;

/// <summary>
/// Represents a test transformation that takes syntax of a PseudoOverride-marked member and injects it.
/// </summary>
internal sealed class TestOverrideDeclarationTransformation : TestTransformationBase, IOverrideDeclarationTransformation
{
    private readonly MemberDeclarationSyntax _syntax;

    public IFullRef<IDeclaration> OverriddenDeclaration { get; }

    public TestOverrideDeclarationTransformation(
        AspectLayerInstance aspectLayerInstance, 
        InsertPosition insertPosition, 
        IFullRef<IDeclaration> overriddenDeclaration, 
        MemberDeclarationSyntax syntax )
        : base( aspectLayerInstance, insertPosition )
    {
        this.OverriddenDeclaration = overriddenDeclaration;
        this._syntax = syntax;
    }

    public override TransformationObservability Observability => TransformationObservability.None;

    public override IRef<IDeclaration> TargetDeclaration => this.OverriddenDeclaration;

    public override SyntaxTree TransformedSyntaxTree => this.OverriddenDeclaration.GetPrimaryDeclarationSyntax().SyntaxTree;

    public override IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context )
    {
        yield return new InjectedMember( this, this._syntax, this.AspectLayerId, InjectedMemberSemantic.Override, this.OverriddenDeclaration );
    }
}
