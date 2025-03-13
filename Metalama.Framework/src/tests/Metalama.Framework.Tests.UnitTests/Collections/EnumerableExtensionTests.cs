// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Collections
{
    public sealed class EnumerableExtensionTests
    {
        [Fact]
        public void AddRange()
        {
            IList<int> l = new List<int>();
            l.AddRange( new[] { 1, 2, 3 } );
            Assert.Equal( 3, l.Count );
        }

        [Fact]
        public void SelectManyRecursiveOnItem()
        {
            Node a = new();
            Node b = new();
            Node c = new( a, b );
            Node d = new( c, b );

            Assert.Equal( new[] { a, b, c }, d.SelectManyRecursiveDistinct( n => n.Children, includeRoot: false ).OrderBy( o => o.Id ) );
            Assert.Equal( new[] { a, b, c, d }, d.SelectManyRecursiveDistinct( n => n.Children, includeRoot: true ).OrderBy( o => o.Id ) );
        }

        [Fact]
        public void SelectManyRecursiveOnList()
        {
            Node a = new();
            Node b = new();
            Node c = new( a, b );
            Node d = new( c, b );
            var list = new[] { d, a };

            Assert.Equal( new[] { a, b, c, d }, list.SelectManyRecursiveDistinct( n => n.Children ).OrderBy( o => o.Id ) );
        }

        [Fact]
        public void ConcatEmptyList()
        {
            var emptyArray = Array.Empty<int>();
            Assert.Single( emptyArray.Concat( 1 ), 1 );
        }

        private sealed class Node
        {
            public int Id { get; } = Interlocked.Increment( ref _nextId );

            private static int _nextId;

            public IReadOnlyList<Node> Children { get; }

            public Node( params Node[] children )
            {
                this.Children = children;
            }
        }
    }
}