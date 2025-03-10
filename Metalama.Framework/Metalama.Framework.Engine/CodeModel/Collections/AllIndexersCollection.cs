// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllIndexersCollection : AllMembersCollection<IIndexer>, IIndexerCollection
{
    public AllIndexersCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IMemberCollection<IIndexer> GetMembers( INamedType namedType ) => namedType.Indexers;

    protected override IEqualityComparer<IIndexer> Comparer => this.CompilationContext.IndexerComparer;
}