// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class MemberRefUpdatableArray<T>
    where T : class, ICompilationElement
{
    // This is the only compilation in which the current object is mutable. It should not be mutable in other transformations.
    public CompilationModel ParentCompilation { get; }

    public MemberRefUpdatableArray( ImmutableArray<IFullRef<T>> array, CompilationModel parentCompilation )
    {
        this.Array = array;
        this.ParentCompilation = parentCompilation;
    }

    public ImmutableArray<IFullRef<T>> Array { get; private set; }

    public void Add( IFullRef<T> member )
    {
        this.Array = this.Array.Add( member );
    }

    public void Remove( IFullRef<T> member )
    {
        var index = this.Array.IndexOf( member, RefEqualityComparer<T>.Default );

        if ( index < 0 )
        {
            throw new AssertionFailedException( $"The collection does not contain '{member}'." );
        }

        this.Array = this.Array.RemoveAt( index );
    }
}