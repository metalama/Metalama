// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

/// <summary>
/// Immutable builder data for introduced extension blocks.
/// </summary>
internal sealed class ExtensionBlockBuilderData : MemberOrNamedTypeBuilderData
{
    private readonly IntroducedRef<IExtensionBlock> _ref;

    /// <summary>
    /// Gets the receiver parameter data (contains type, name, ref kind, attributes).
    /// </summary>
    public ParameterBuilderData ReceiverParameter { get; }

    /// <summary>
    /// Gets the type parameters of this extension block.
    /// </summary>
    public ImmutableArray<TypeParameterBuilderData> TypeParameters { get; }

    /// <summary>
    /// Gets a value indicating whether this is a static extension (no parameter name).
    /// </summary>
    public bool IsStaticExtension => string.IsNullOrEmpty( this.ReceiverParameter.Name );

    public ExtensionBlockBuilderData( ExtensionBlockBuilder builder, IFullRef<IDeclaration> containingDeclaration )
        : base( builder, containingDeclaration )
    {
        this._ref = builder.Ref;
        this.ReceiverParameter = builder.ReceiverParameterBuilder.BuilderData;
        this.TypeParameters = builder.TypeParameters.ToImmutable( this._ref );
        this.Attributes = builder.Attributes.ToImmutable( this._ref );
    }

    public override DeclarationKind DeclarationKind => DeclarationKind.ExtensionBlock;

    protected override IFullRef<IDeclaration> ToDeclarationFullRef() => this._ref;

    public new IFullRef<IExtensionBlock> ToRef() => this._ref;

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations()
        => base.GetOwnedDeclarations().Concat( this.TypeParameters ).Append( this.ReceiverParameter );
}
#endif