// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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