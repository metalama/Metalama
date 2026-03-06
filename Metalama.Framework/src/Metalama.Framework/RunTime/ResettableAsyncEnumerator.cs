// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// An <see cref="IAsyncEnumerator{T}"/> wrapper around an <see cref="AsyncEnumerableList{T}"/> that supports resetting.
    /// This type is used by code transformed by aspects when return value contracts are applied to async iterator methods
    /// that return <see cref="IAsyncEnumerator{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    public sealed class ResettableAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly AsyncEnumerableList<T> _list;
        private readonly CancellationToken _cancellationToken;
        private List<T>.Enumerator _enumerator;

        internal ResettableAsyncEnumerator( AsyncEnumerableList<T> list, CancellationToken cancellationToken )
        {
            this._list = list;
            this._cancellationToken = cancellationToken;
            this._enumerator = list.GetEnumerator();
        }

        /// <summary>
        /// Gets the parent list.
        /// </summary>
        public AsyncEnumerableList<T> Parent => this._list;

        /// <inheritdoc />
        public T Current => this._enumerator.Current;

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync()
        {
            this._cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult( this._enumerator.MoveNext() );
        }

        /// <summary>
        /// Resets the enumerator to the beginning of the list.
        /// </summary>
        public void Reset()
        {
            this._enumerator = this._list.GetEnumerator();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
#endif
