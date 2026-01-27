// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using Accessibility = Metalama.Framework.Code.Accessibility;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builder for introducing extension blocks into static classes.
/// </summary>
internal sealed class ExtensionBlockBuilder : NamedTypeBuilder, IExtensionBlockBuilder
{
    /// <summary>
    /// Gets the receiver parameter builder.
    /// </summary>
    public ExtensionReceiverParameterBuilder ReceiverParameterBuilder { get; }

    /// <summary>
    /// Gets a new reference for this extension block.
    /// </summary>
    public new IntroducedRef<IExtensionBlock> Ref { get; }

    /// <summary>
    /// Gets a value indicating whether this is a static extension (no parameter name).
    /// </summary>
    public bool IsStaticExtension => string.IsNullOrEmpty( this.ReceiverParameterBuilder.Name );

    public ExtensionBlockBuilder(
        AspectLayerInstance aspectLayerInstance,
        INamedType declaringType,
        IType receiverType,
        string? receiverParameterName )
        : base(
            aspectLayerInstance,
            declaringType,
            "", // Extension blocks don't have names.
            TypeKind.Extension )
    {
        Invariant.Assert( declaringType.IsStatic, "Extension blocks can only be introduced into static classes." );

        this.ReceiverParameterBuilder = new ExtensionReceiverParameterBuilder(
            this,
            this.Compilation,
            aspectLayerInstance,
            receiverType,
            receiverParameterName );

        this.Ref = new IntroducedRef<IExtensionBlock>( this.Compilation.RefFactory );
    }

    #region IExtensionBlockBuilder Implementation

    IParameterBuilder IExtensionBlockBuilder.ReceiverParameter => this.ReceiverParameterBuilder;

    #endregion

    #region IExtensionBlock Implementation

    IType IExtensionBlock.ReceiverType => this.ReceiverParameterBuilder.Type;

    IParameter IExtensionBlock.ReceiverParameter => this.ReceiverParameterBuilder;

    IRef<IExtensionBlock> IExtensionBlock.ToRef() => this.Ref;

    INamedType IExtensionBlock.DeclaringType => this.DeclaringType.AssertNotNull();

    #endregion

    #region Restricted Operations - throw NotSupportedException

    public override INamedType? BaseType
    {
        get => null;
        set => throw new NotSupportedException( "Extension blocks cannot have a base type." );
    }

    public override string Name
    {
        get => this._name;
        set => this._name = value;
    }

    private string _name = "";

    public override Accessibility Accessibility
    {
        get => Accessibility.Public; // Roslyn reports Public for extension blocks.
        set => throw new NotSupportedException( "Extension blocks do not have accessibility modifiers." );
    }

    public override bool IsAbstract
    {
        get => false;
        set => throw new NotSupportedException( "Extension blocks cannot be abstract." );
    }

    public override bool IsSealed
    {
        get => false;
        set => throw new NotSupportedException( "Extension blocks cannot be sealed." );
    }

    public override bool IsPartial
    {
        get => false;
        set => throw new NotSupportedException( "Extension blocks cannot be partial." );
    }

    public override bool IsStatic
    {
        get => true; // Always effectively static (inside static class).
        set => throw new NotSupportedException( "Extension blocks are always implicitly static." );
    }

    #endregion

    public override DeclarationKind DeclarationKind => DeclarationKind.ExtensionBlock;

    public override bool CanBeInherited => false;

    protected override void InitializeBaseType()
    {
        // Extension blocks don't have a base type.
    }

    protected override void FreezeChildren()
    {
        base.FreezeChildren();
        this.ReceiverParameterBuilder.Freeze();
    }

    protected override void EnsureReferenceInitialized()
    {
        this.Ref.BuilderData = new ExtensionBlockBuilderData( this, this.ContainingDeclaration.ToFullRef() );
    }

    public new ExtensionBlockBuilderData BuilderData => (ExtensionBlockBuilderData) this.Ref.BuilderData;

    protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;
}
#endif