// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyFieldOrPropertyCollection : IFieldOrPropertyCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public EmptyFieldOrPropertyCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IFieldOrProperty> OfName( string name ) => Array.Empty<IFieldOrProperty>();

    public IEnumerator<IFieldOrProperty> GetEnumerator() => ((IEnumerable<IFieldOrProperty>) Array.Empty<IFieldOrProperty>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}