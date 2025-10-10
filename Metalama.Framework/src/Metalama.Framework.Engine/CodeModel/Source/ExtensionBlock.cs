// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class ExtensionBlock : SourceNamedType, IExtensionBlock
{
    internal ExtensionBlock( INamedTypeSymbol typeSymbol, CompilationModel compilation ) : base(
        typeSymbol,
        compilation,
        null,
        new ExtensionBlockImpl( typeSymbol, compilation ) ) { }

    public IType ReceiverType
    {
        get
        {
            this.OnUsingDeclaration();

            return ((ExtensionBlockImpl) this.Implementation).ReceiverType;
        }
    }

    public IParameter ReceiverParameter
    {
        get
        {
            this.OnUsingDeclaration();

            return ((ExtensionBlockImpl) this.Implementation).ReceiverParameter;
        }
    }

    public new IRef<IExtensionBlock> ToRef() => base.ToRef().As<IExtensionBlock>();

    public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();
}
#endif