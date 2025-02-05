// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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