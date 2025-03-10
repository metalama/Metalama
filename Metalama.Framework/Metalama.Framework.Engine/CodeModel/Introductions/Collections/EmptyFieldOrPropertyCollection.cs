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