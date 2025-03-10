// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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