// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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