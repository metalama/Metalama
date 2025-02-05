// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyIndexerCollection : IIndexerCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public EmptyIndexerCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IIndexer> OfName( string name ) => Array.Empty<IIndexer>();

    public IEnumerator<IIndexer> GetEnumerator() => ((IEnumerable<IIndexer>) Array.Empty<IIndexer>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}