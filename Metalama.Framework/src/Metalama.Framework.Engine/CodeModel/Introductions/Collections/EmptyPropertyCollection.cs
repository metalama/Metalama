// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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