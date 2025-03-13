// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
