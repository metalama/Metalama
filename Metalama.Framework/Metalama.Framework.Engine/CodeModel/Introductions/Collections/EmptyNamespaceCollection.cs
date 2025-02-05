// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyNamespaceCollection : INamespaceCollection
{
    public int Count => 0;

    public INamespace? OfName( string name ) => null;

    IEnumerable<INamespace> INamedDeclarationCollection<INamespace>.OfName( string name ) => [];

    public IEnumerator<INamespace> GetEnumerator() => ((IEnumerable<INamespace>) Array.Empty<INamespace>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}