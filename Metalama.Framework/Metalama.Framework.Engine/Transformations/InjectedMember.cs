// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Linking;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Transformations;

// TODO: This class is misused for injection of types into introduced namespaces. It should be refactored after implementation of targeting specific syntax trees.

/// <summary>
/// Represents a member to be introduced in a type and encapsulates the information needed by the <see cref="AspectLinker"/>
/// to perform the linking.
/// </summary>
internal sealed class InjectedMember
{
    public DeclarationKind Kind { get; }

    /// <summary>
    /// Gets the <see cref="ISyntaxTreeTransformation" /> that created this object.
    /// </summary>
    public ISyntaxTreeTransformation? Transformation { get; }

    public DeclarationBuilderData? BuilderData => (this.Transformation as IIntroduceDeclarationTransformation)?.DeclarationBuilderData;

    /// <summary>
    /// Gets the syntax of the introduced member.
    /// </summary>
    public MemberDeclarationSyntax Syntax { get; }

    /// <summary>
    /// Gets the <see cref="AspectLayerId"/> that emitted the current <see cref="InjectedMember"/>.
    /// </summary>
    public AspectLayerId? AspectLayerId { get; }

    /// <summary>
    /// Gets the semantic of the introduced member as supported by the linker.
    /// </summary>
    public InjectedMemberSemantic Semantic { get; }

    /// <summary>
    /// Gets the declaration (overriden or introduced) that corresponds to the current <see cref="InjectedMember"/>.
    /// This is used to associate diagnostic suppressions to the introduced member. If <c>null</c>, diagnostics
    /// are not suppressed from the introduced member.
    /// </summary>
    public IFullRef<IDeclaration> Declaration { get; }

    public SyntaxTree GetTargetSyntaxTree()
        => this.Transformation != null
            ? this.Transformation.TransformedSyntaxTree
            : this.Declaration.PrimarySyntaxTree.AssertNotNull();

    public InjectedMember(
        IInjectMemberTransformation injectMemberTransformation,
        MemberDeclarationSyntax syntax,
        AspectLayerId? aspectLayerId,
        InjectedMemberSemantic semantic,
        IFullRef<IDeclaration> declaration ) : this(
        injectMemberTransformation,
        declaration.DeclarationKind,
        syntax,
        aspectLayerId,
        semantic,
        declaration ) { }

    public InjectedMember(
        OverrideMemberTransformation overrideMemberTransformation,
        MemberDeclarationSyntax syntax,
        AspectLayerId aspectLayerId,
        InjectedMemberSemantic semantic,
        IFullRef<IDeclaration> declaration ) : this(
        overrideMemberTransformation,
        overrideMemberTransformation.OverriddenDeclaration.DeclarationKind,
        syntax,
        aspectLayerId,
        semantic,
        declaration ) { }

    internal InjectedMember(
        ISyntaxTreeTransformation? transformation,
        DeclarationKind kind,
        MemberDeclarationSyntax syntax,
        AspectLayerId? aspectLayerId,
        InjectedMemberSemantic semantic,
        IFullRef<IDeclaration> declaration )
    {
        this.Transformation = transformation;
        this.Syntax = syntax;
        this.AspectLayerId = aspectLayerId;
        this.Semantic = semantic;
        this.Declaration = declaration;
        this.Kind = kind;
    }

    public override string ToString() => this.Transformation?.ToString() ?? "(linker auxiliary)";
}