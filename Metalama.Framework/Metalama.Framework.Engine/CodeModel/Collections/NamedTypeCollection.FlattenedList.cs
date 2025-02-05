// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.UpdatableCollections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed partial class NamedTypeCollection
{
    private sealed class FlattenedList : List<IFullRef<INamedType>>, IUpdatableCollection<IFullRef<INamedType>>
    {
        public FlattenedList( CompilationModel compilation, IReadOnlyList<IFullRef<INamedType>> source )
        {
            this.Compilation = compilation;
            this.Capacity = source.Count;

            // TODO: We are effectively caching the source collection, but this collection can change. It seems it does not break any use case at the moment.

            foreach ( var item in source )
            {
                AddRecursively( item );
            }

            void AddRecursively( IFullRef<INamedType> item )
            {
                this.Add( item );

                foreach ( var nestedType in compilation.GetNamedTypeCollectionByParent( item ) )
                {
                    AddRecursively( nestedType );
                }
            }
        }

        public CompilationModel Compilation { get; }

        public IUpdatableCollection Clone( CompilationModel compilation ) => throw new NotSupportedException();

        public ImmutableArray<IFullRef<INamedType>> OfName( string name ) => this.Where( r => r.Name == name ).ToImmutableArray();
    }
}