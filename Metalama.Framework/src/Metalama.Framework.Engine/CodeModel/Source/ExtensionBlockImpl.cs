// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class ExtensionBlockImpl : SourceNamedTypeImpl, IExtensionBlock
{
    internal ExtensionBlockImpl( INamedTypeSymbol namedTypeSymbol, CompilationModel compilation ) : base(
        namedTypeSymbol,
        compilation,
        null ) { }

    [Memo]
    public IType ReceiverType => this.Compilation.Factory.GetIType( this.NamedTypeSymbol.ExtensionParameter.AssertSymbolNotNull().Type );

    [Memo]
    public IParameter ReceiverParameter => this.Compilation.Factory.GetParameter( this.NamedTypeSymbol.ExtensionParameter.AssertSymbolNotNull() );

    public IRef<IExtensionBlock> ToRef() => this.Ref.AsFullRef<IExtensionBlock>();

    public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();

    public override TypeKind TypeKind => TypeKind.Extension;
    
    protected override void CheckSymbol()
    {
        Invariant.Assert( this.NamedTypeSymbol.IsExtension );
    }

    protected override IFullRef<INamedType> CreateFullRef() => this.RefFactory.FromSymbolBasedDeclaration<IExtensionBlock>( this ).As<INamedType>();
}
#endif