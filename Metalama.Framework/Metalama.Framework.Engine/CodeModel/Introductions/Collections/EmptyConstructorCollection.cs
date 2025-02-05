// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyConstructorCollection : IConstructorCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public EmptyConstructorCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IConstructor> OfName( string name ) => Array.Empty<IConstructor>();

    public IEnumerator<IConstructor> GetEnumerator() => ((IEnumerable<IConstructor>) Array.Empty<IConstructor>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}