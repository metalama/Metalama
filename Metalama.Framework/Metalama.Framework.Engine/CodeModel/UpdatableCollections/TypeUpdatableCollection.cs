// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class TypeUpdatableCollection : NonUniquelyNamedUpdatableCollection<INamedType>
{
    public TypeUpdatableCollection( CompilationModel compilation, IRef<INamespaceOrNamedType> declaringTypeOrNamespace ) : base(
        compilation,
        declaringTypeOrNamespace ) { }

    public TypeUpdatableCollection( CompilationModel compilation ) : base(
        compilation,
        compilation.ToRef() ) { }

    protected override IEqualityComparer<IRef<INamedType>> MemberRefComparer => RefEqualityComparer<INamedType>.Default;

    protected override DeclarationKind ItemsDeclarationKind => DeclarationKind.NamedType;
}