// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// A <see cref="List{T}"/> that implements <see cref="IAsyncEnumerable{T}"/>. This class is used when a non-iterator template is applied
    /// to an async iterator method.
    /// </summary>
    /// <typeparam name="T">Type of items.</typeparam>
    public sealed class AsyncEnumerableList<T> : List<T>, IAsyncEnumerable<T>
    {
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator( CancellationToken cancellationToken ) => this.GetAsyncEnumerator( cancellationToken );

        /// <summary>
        /// Gets an enumerator for the current list.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [PublicAPI]
        public AsyncEnumerator GetAsyncEnumerator( CancellationToken cancellationToken = default ) => new( this, cancellationToken );

        /// <summary>
        /// Implementation of <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        public struct AsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly CancellationToken _cancellationToken;
            private Enumerator _enumerator;

            public AsyncEnumerator( AsyncEnumerableList<T> parent, CancellationToken cancellationToken )
            {
                this.Parent = parent;
                this._enumerator = parent.GetEnumerator();
                this._cancellationToken = cancellationToken;
            }

            public AsyncEnumerableList<T> Parent { get; }

            // The static members of ValueTask are not available on netstandard2.0, so the default value, which represents a
            // completed operation, is returned instead of ValueTask.CompletedTask.
            public ValueTask DisposeAsync() => default;

            public ValueTask<bool> MoveNextAsync()
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                // The static members of ValueTask are not available on netstandard2.0, so the value is wrapped by the
                // constructor instead of by ValueTask.FromResult.
                return new ValueTask<bool>( this._enumerator.MoveNext() );
            }

            public T Current => this._enumerator.Current;
        }
    }
}
