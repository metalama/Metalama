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

internal sealed class EmptyMethodCollection : IMethodCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public EmptyMethodCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IMethod> OfName( string name ) => Array.Empty<IMethod>();

    public IEnumerator<IMethod> GetEnumerator() => ((IEnumerable<IMethod>) Array.Empty<IMethod>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public IEnumerable<IMethod> OfKind( MethodKind kind ) => Array.Empty<IMethod>();

    public IEnumerable<IMethod> OfKind( OperatorKind kind ) => Array.Empty<IMethod>();
}