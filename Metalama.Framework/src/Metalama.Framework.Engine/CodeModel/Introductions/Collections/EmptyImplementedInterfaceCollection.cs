// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyImplementedInterfaceCollection : IImplementedInterfaceCollection
{
    public int Count => 0;

    public bool Contains( INamedType namedType ) => false;

    public bool Contains( Type type ) => false;

    public IEnumerator<INamedType> GetEnumerator() => ((IEnumerable<INamedType>) Array.Empty<INamedType>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}