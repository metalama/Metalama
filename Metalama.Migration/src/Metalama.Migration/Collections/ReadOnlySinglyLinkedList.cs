// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PostSharp.Collections
{
    [PublicAPI]
    public struct ReadOnlySinglyLinkedList<T> : IEnumerable<T>
    {
        public ISinglyLinkedListNode<T> FirstNode { get; }

        public bool IsEmpty { get; }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public Enumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [PublicAPI]
        public struct Enumerator : IEnumerator<T>
        {
            public void Dispose() { }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            public T Current => throw new NotImplementedException();

            object IEnumerator.Current { get; }
        }
    }
}