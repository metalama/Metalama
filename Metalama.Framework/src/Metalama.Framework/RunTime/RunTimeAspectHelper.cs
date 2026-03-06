// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable RedundantBlankLines, MissingBlankLines

using System;
using System.Collections;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;

#endif

// ReSharper restore RedundantBlankLines, MissingBlankLines

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// Defines helper methods used by code transformed by aspects.
    /// </summary>
    public static class RunTimeAspectHelper
    {
        /// <summary>
        /// Evaluates an <see cref="IEnumerable{T}"/> and stores the result into a <see cref="List{T}"/>. If the enumerable is already
        /// a list, returns the input list. The intended side effect of this method is to completely evaluate the input enumerable.
        /// </summary>
        /// <param name="enumerable">An enumerable.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <see cref="List{T}"/> made from the items of <paramref name="enumerable"/>, or the <paramref name="enumerable"/> object itself
        /// it is already a <see cref="List{T}"/>.</returns>
        public static List<T> Buffer<T>( this IEnumerable<T> enumerable ) => enumerable as List<T> ?? new List<T>( enumerable );

        /// <summary>
        /// Evaluates an <see cref="IEnumerable"/> and stores the result into a <c>List&lt;object&gt;</c>. If the enumerable is already
        /// a list, returns the input list.  The intended side effect of this method is to completely evaluate the input enumerable.
        /// </summary>
        /// <param name="enumerable">An enumerable.</param>
        /// <returns>A <c>List&lt;object&gt;</c> made from the items of <paramref name="enumerable"/>, or the <paramref name="enumerable"/> object itself
        /// it is already a <c>List&lt;object&gt;</c>.</returns>
        public static List<object> Buffer( this IEnumerable enumerable )
        {
            if ( enumerable is List<object> list )
            {
                return list;
            }
            else
            {
                list = new List<object>();

                foreach ( var item in enumerable )
                {
                    list.Add( item );
                }

                return list;
            }
        }

        /// <summary>
        /// Evaluates an <see cref="IEnumerator{T}"/>, stores the result into a <see cref="List{T}"/> and returns a resettable enumerator for this list.
        /// If the enumerator is already buffered, resets it and returns the same instance.
        /// The intended side effect of this method is to completely evaluate the input enumerator.
        /// </summary>
        /// <param name="enumerator">An enumerator.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An <see cref="IEnumerator{T}"/> that supports <see cref="IEnumerator.Reset"/>.</returns>
        public static IEnumerator<T> Buffer<T>( this IEnumerator<T> enumerator )
        {
            if ( enumerator is Enumerator<T> resettableEnumerator )
            {
                resettableEnumerator.Reset();

                return resettableEnumerator;
            }
            else
            {
                List<T> list = new();

                try
                {
                    while ( enumerator.MoveNext() )
                    {
                        list.Add( enumerator.Current );
                    }
                }
                finally
                {
                    enumerator.Dispose();
                }

                return new Enumerator<T>( list );
            }
        }

        /// <summary>
        /// Evaluates an <see cref="IEnumerator"/>, stores the result into a <c>List&lt;object?&gt;</c> and returns a resettable enumerator for this list.
        /// If the enumerator is already buffered, resets it and returns the same instance.
        /// The intended side effect of this method is to completely evaluate the input enumerator.
        /// </summary>
        /// <param name="enumerator">An enumerator.</param>
        /// <returns>An <see cref="IEnumerator"/> that supports <see cref="IEnumerator.Reset"/>.</returns>
        public static IEnumerator Buffer( this IEnumerator enumerator )
        {
            if ( enumerator is Enumerator resettableEnumerator )
            {
                resettableEnumerator.Reset();

                return resettableEnumerator;
            }
            else
            {
                List<object?> list = new();

                try
                {
                    while ( enumerator.MoveNext() )
                    {
                        list.Add( enumerator.Current );
                    }
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }

                return new Enumerator( list );
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Evaluates an <see cref="IAsyncEnumerable{T}"/> and stores the result into an <see cref="AsyncEnumerableList{T}"/>. If the enumerable is already
        /// an <see cref="AsyncEnumerableList{T}"/>, returns the input list.
        ///  The intended side effect of this method is to completely evaluate the input enumerable.
        /// </summary>
        /// <param name="enumerable">An enumerable.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <see cref="AsyncEnumerableList{T}"/> made from the items of <paramref name="enumerable"/>, or the <paramref name="enumerable"/> object itself
        /// it is already an <see cref="AsyncEnumerableList{T}"/>.</returns>
        public static async ValueTask<AsyncEnumerableList<T>> BufferAsync<T>(
            this IAsyncEnumerable<T> enumerable,
            CancellationToken cancellationToken = default )
        {
            if ( enumerable is AsyncEnumerableList<T> asyncEnumerableList )
            {
                return asyncEnumerableList;
            }
            else
            {
                asyncEnumerableList = new AsyncEnumerableList<T>();

                await foreach ( var item in enumerable.WithCancellation( cancellationToken ) )
                {
                    asyncEnumerableList.Add( item );
                }

                return asyncEnumerableList;
            }
        }

        /// <summary>
        /// Evaluates an <see cref="IAsyncEnumerator{T}"/>, stores the result into an <see cref="AsyncEnumerableList{T}"/> and returns a resettable async enumerator for this object.
        /// If the enumerator is already buffered, resets it and returns the same instance.
        /// The intended side effect of this method is to completely evaluate the input enumerator.
        /// </summary>
        /// <param name="enumerator">An enumerator.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An <see cref="IAsyncEnumerator{T}"/> that supports resetting via a subsequent call to this method.</returns>
        public static async ValueTask<IAsyncEnumerator<T>> BufferAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            CancellationToken cancellationToken
                = default )
        {
            if ( enumerator is AsyncEnumerator<T> resettableEnumerator )
            {
                resettableEnumerator.Reset();

                return resettableEnumerator;
            }
            else
            {
                var list = new AsyncEnumerableList<T>();

                try
                {
                    while ( await enumerator.MoveNextAsync() )
                    {
                        list.Add( enumerator.Current );

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }

                return new AsyncEnumerator<T>( list, cancellationToken );
            }
        }

        /// <summary>
        /// Evaluates an <see cref="IAsyncEnumerator{T}"/>, stores the result into an <see cref="AsyncEnumerableList{T}"/> and returns the list.
        /// If the enumerator is already an <see cref="AsyncEnumerableList{T}"/> enumerator, returns the parent <see cref="AsyncEnumerableList{T}"/>.
        ///  The intended side effect of this method is to completely evaluate the input enumerator.
        /// </summary>
        /// <param name="enumerator">An enumerator.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An <see cref="AsyncEnumerableList{T}"/> made from the items of <paramref name="enumerator"/>, or the parent <see cref="AsyncEnumerableList{T}"/> object
        /// if the <paramref name="enumerator"/> object itself it is already an <see cref="AsyncEnumerableList{T}"/> enumerator.</returns>
        public static async ValueTask<AsyncEnumerableList<T>> BufferToListAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            CancellationToken cancellationToken = default )
        {
            if ( enumerator is AsyncEnumerator<T> resettableEnumerator )
            {
                return resettableEnumerator.Parent;
            }
            else if ( enumerator is AsyncEnumerableList<T>.AsyncEnumerator typedEnumerator )
            {
                return typedEnumerator.Parent;
            }
            else
            {
                var list = new AsyncEnumerableList<T>();

                await using ( enumerator )
                {
                    while ( await enumerator.MoveNextAsync() )
                    {
                        list.Add( enumerator.Current );

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                return list;
            }
        }
#endif

        /// <summary>
        /// An <see cref="IEnumerator{T}"/> wrapper around a <see cref="List{T}"/> that supports <see cref="IEnumerator.Reset"/>.
        /// This type is used by code transformed by aspects when return value contracts are applied to iterator methods
        /// that return <see cref="IEnumerator{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of items.</typeparam>
        internal sealed class Enumerator<T> : IEnumerator<T>
        {
            private readonly List<T> _list;
            private List<T>.Enumerator _enumerator;

            internal Enumerator( List<T> list )
            {
                this._list = list;
                this._enumerator = list.GetEnumerator();
            }

            /// <inheritdoc />
            public T Current => this._enumerator.Current;

            /// <inheritdoc />
            object? IEnumerator.Current => this.Current;

            /// <inheritdoc />
            public bool MoveNext() => this._enumerator.MoveNext();

            /// <inheritdoc />
            public void Reset()
            {
                this._enumerator = this._list.GetEnumerator();
            }

            /// <inheritdoc />
            public void Dispose()
            {
                this._enumerator.Dispose();
            }
        }

        /// <summary>
        /// An <see cref="IEnumerator"/> wrapper around a <c>List&lt;object?&gt;</c> that supports <see cref="IEnumerator.Reset"/>.
        /// This type is used by code transformed by aspects when return value contracts are applied to iterator methods
        /// that return <see cref="IEnumerator"/>.
        /// </summary>
        internal sealed class Enumerator : IEnumerator
        {
            private readonly List<object?> _list;
            private List<object?>.Enumerator _enumerator;

            internal Enumerator( List<object?> list )
            {
                this._list = list;
                this._enumerator = list.GetEnumerator();
            }

            /// <inheritdoc />
            public object? Current => this._enumerator.Current;

            /// <inheritdoc />
            public bool MoveNext() => this._enumerator.MoveNext();

            /// <inheritdoc />
            public void Reset()
            {
                this._enumerator = this._list.GetEnumerator();
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// An <see cref="IAsyncEnumerator{T}"/> wrapper around an <see cref="AsyncEnumerableList{T}"/> that supports resetting.
        /// This type is used by code transformed by aspects when return value contracts are applied to async iterator methods
        /// that return <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of items.</typeparam>
        internal sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly AsyncEnumerableList<T> _list;
            private readonly CancellationToken _cancellationToken;
            private List<T>.Enumerator _enumerator;

            internal AsyncEnumerator( AsyncEnumerableList<T> list, CancellationToken cancellationToken )
            {
                this._list = list;
                this._cancellationToken = cancellationToken;
                this._enumerator = list.GetEnumerator();
            }

            /// <summary>
            /// Gets the parent list.
            /// </summary>
            internal AsyncEnumerableList<T> Parent => this._list;

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
            internal void Reset()
            {
                this._enumerator = this._list.GetEnumerator();
            }

            /// <inheritdoc />
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
#endif
    }
}
