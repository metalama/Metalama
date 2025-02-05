// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyPropertyCollection : IPropertyCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public IProperty this[ string name ] => throw new InvalidOperationException();

    public EmptyPropertyCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IProperty> OfName( string name ) => Array.Empty<IProperty>();

    public IEnumerator<IProperty> GetEnumerator() => ((IEnumerable<IProperty>) Array.Empty<IProperty>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}