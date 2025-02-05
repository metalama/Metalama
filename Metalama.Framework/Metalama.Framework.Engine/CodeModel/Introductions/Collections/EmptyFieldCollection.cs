// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class EmptyFieldCollection : IFieldCollection
{
    public int Count => 0;

    public INamedType DeclaringType { get; }

    public IField this[ string name ] => throw new InvalidOperationException();

    public EmptyFieldCollection( INamedType declaringType )
    {
        this.DeclaringType = declaringType;
    }

    public IEnumerable<IField> OfName( string name ) => Array.Empty<IField>();

    public IEnumerator<IField> GetEnumerator() => ((IEnumerable<IField>) Array.Empty<IField>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}