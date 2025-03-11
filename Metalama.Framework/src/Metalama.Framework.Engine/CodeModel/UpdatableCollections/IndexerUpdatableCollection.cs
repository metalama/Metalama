// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class IndexerUpdatableCollection : NonUniquelyNamedUpdatableCollection<IIndexer>
{
    public IndexerUpdatableCollection( CompilationModel compilation, IFullRef<INamedType> declaringType ) : base(
        compilation,
        declaringType.As<INamespaceOrNamedType>() ) { }

    protected override IEqualityComparer<IRef<IIndexer>> MemberRefComparer => RefEqualityComparer<IIndexer>.Default;

    protected override DeclarationKind ItemsDeclarationKind => DeclarationKind.Indexer;
}