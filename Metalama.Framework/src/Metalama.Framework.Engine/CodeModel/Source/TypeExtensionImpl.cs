// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class TypeExtensionImpl : SourceNamedTypeImpl, ITypeExtension
{
    internal TypeExtensionImpl( INamedTypeSymbol namedTypeSymbol, CompilationModel compilation ) : base(
        namedTypeSymbol,
        compilation,
        null ) { }

    [Memo]
    public IType ExtendedType => this.Compilation.Factory.GetIType( this.NamedTypeSymbol.ExtensionParameter.AssertSymbolNotNull().Type );

    [Memo]
    public IParameter ExtensionParameter => this.Compilation.Factory.GetParameter( this.NamedTypeSymbol.ExtensionParameter.AssertSymbolNotNull() );

    public IRef<ITypeExtension> ToRef() => this.Ref.AsFullRef<ITypeExtension>();

    public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();

    public override TypeKind TypeKind => TypeKind.Extension;
    
    protected override void CheckSymbol()
    {
        Invariant.Assert( this.NamedTypeSymbol.IsExtension );
    }

    protected override IFullRef<INamedType> CreateFullRef() => this.RefFactory.FromSymbolBasedDeclaration<ITypeExtension>( this );
}