// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyEventCollection : IEventCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public EmptyEventCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IEvent> OfName( string name ) => Array.Empty<IEvent>();

    public IEnumerator<IEvent> GetEnumerator() => ((IEnumerable<IEvent>) Array.Empty<IEvent>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}