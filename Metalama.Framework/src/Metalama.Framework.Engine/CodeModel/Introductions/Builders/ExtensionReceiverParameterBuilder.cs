// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Builders;

/// <summary>
/// Builder for the receiver parameter of an extension block.
/// </summary>
internal sealed class ExtensionReceiverParameterBuilder : BaseParameterBuilder
{
    private readonly ExtensionBlockBuilder _extensionBlock;
    private IType _type;
    private string _name;
    private RefKind _refKind;

    public ExtensionReceiverParameterBuilder(
        ExtensionBlockBuilder extensionBlock,
        CompilationModel compilation,
        AspectLayerInstance aspectLayerInstance,
        IType receiverType,
        string? receiverParameterName )
        : base( compilation, aspectLayerInstance )
    {
        this._extensionBlock = extensionBlock;
        this._type = receiverType;
        this._name = receiverParameterName ?? "";
        this._refKind = RefKind.None;
    }

    #region IParameterBuilder Implementation

    public override IType Type
    {
        get => this._type;
        set
        {
            this.CheckNotFrozen();
            this._type = value;
        }
    }

    public override string Name
    {
        get => this._name;
        set
        {
            this.CheckNotFrozen();
            this._name = value ?? "";
        }
    }

    public override RefKind RefKind
    {
        get => this._refKind;
        set
        {
            this.CheckNotFrozen();

            if ( value == RefKind.Out )
            {
                throw new ArgumentOutOfRangeException( nameof(value), "Extension block receiver parameters cannot use 'out'. Use 'ref' or 'in' instead." );
            }

            this._refKind = value;
        }
    }

    public override int Index => 0;

    public override bool IsReturnParameter => false;

    public override IHasParameters? DeclaringMember => null;

    public override IDeclaration ContainingDeclaration => this._extensionBlock;

    public override ParameterInfo ToParameterInfo() => throw new NotSupportedException( "ToParameterInfo is not supported for extension receiver parameters." );

    #endregion

    #region Restricted Operations

    public override TypedConstant? DefaultValue
    {
        get => null;
        set => throw new NotSupportedException( "Extension receiver parameters cannot have default values." );
    }

    public override bool IsParams
    {
        get => false;
        set => throw new NotSupportedException( "Extension receiver parameters cannot be params." );
    }

    public override bool IsThis
    {
        get => false; // Extension receivers are not 'this' parameters.
        set => throw new NotSupportedException( "Extension receiver parameters do not use the 'this' modifier." );
    }

    #endregion

    /// <summary>
    /// Gets a value indicating whether this creates a static extension (no parameter name).
    /// </summary>
    public bool IsStaticExtension => string.IsNullOrEmpty( this._name );

    public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

    public override bool CanBeInherited => false;
}
#endif