// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.ObjectModel;

namespace Metalama.Framework.Engine.ReferenceGraph;

public sealed class ReferencingNodeList : Collection<ReferencingNode>
{
    public ReferenceKinds ReferenceKinds { get; private set; }

    protected override void InsertItem( int index, ReferencingNode item )
    {
        this.ReferenceKinds |= item.ReferenceKind;
        base.InsertItem( index, item );
    }

    protected override void ClearItems() => throw new NotSupportedException();

    protected override void SetItem( int index, ReferencingNode item ) => throw new NotSupportedException();

    protected override void RemoveItem( int index ) => throw new NotSupportedException();
}