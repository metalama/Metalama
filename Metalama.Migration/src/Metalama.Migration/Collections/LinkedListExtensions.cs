// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace PostSharp.Collections
{
    public static class LinkedListExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>( IReadOnlyLinkedList<T> linkedList )
        {
            if ( linkedList == null )
            {
                yield break;
            }

            for ( var cursor = linkedList.First; cursor != null; cursor = cursor.Next )
            {
                yield return cursor.Value;
            }
        }

        public static IEnumerable<T> ToEnumerable<T>( ReadOnlyLinkedList<T> linkedList )
        {
            for ( var cursor = linkedList.First; cursor != null; cursor = cursor.Next )
            {
                yield return cursor.Value;
            }
        }
    }
}