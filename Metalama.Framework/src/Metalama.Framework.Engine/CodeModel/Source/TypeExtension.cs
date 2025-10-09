// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class TypeExtension : SourceNamedType, ITypeExtension
{
    internal TypeExtension( INamedTypeSymbol typeSymbol, CompilationModel compilation ) : base( typeSymbol, compilation, null, new TypeExtensionImpl( typeSymbol, compilation  ) ) { }

    public IType ExtendedType
    {
        get
        {
            this.OnUsingDeclaration();

            return ((TypeExtensionImpl) this.Implementation).ExtendedType;
        }
    }

    public IParameter ExtensionParameter
    {
        get
        {
            this.OnUsingDeclaration();

            return ((TypeExtensionImpl) this.Implementation).ExtensionParameter;
        }
    }

    public new IRef<ITypeExtension> ToRef() => base.ToRef().As<ITypeExtension>();

    public INamedType DeclaringType => base.DeclaringType.AssertNotNull();
}