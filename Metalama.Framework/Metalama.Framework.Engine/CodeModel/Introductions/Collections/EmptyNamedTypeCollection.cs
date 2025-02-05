// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyNamedTypeCollection : INamedTypeCollection
{
    public int Count => 0;

    public IEnumerable<INamedType> OfName( string name ) => Array.Empty<INamedType>();

    public IEnumerable<INamedType> OfTypeDefinition( INamedType typeDefinition ) => Array.Empty<INamedType>();

    public IEnumerator<INamedType> GetEnumerator() => ((IEnumerable<INamedType>) Array.Empty<INamedType>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}