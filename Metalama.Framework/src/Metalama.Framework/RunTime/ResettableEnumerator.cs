// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// An <see cref="IEnumerator{T}"/> wrapper around a <see cref="List{T}"/> that supports <see cref="IEnumerator.Reset"/>.
    /// This type is used by code transformed by aspects when return value contracts are applied to iterator methods
    /// that return <see cref="IEnumerator{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items.</typeparam>
    public sealed class ResettableEnumerator<T> : IEnumerator<T>
    {
        private readonly List<T> _list;
        private List<T>.Enumerator _enumerator;

        internal ResettableEnumerator( List<T> list )
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

        /// <summary>
        /// Resets the enumerator to the beginning of the list.
        /// </summary>
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
    public sealed class ResettableEnumerator : IEnumerator
    {
        private readonly List<object?> _list;
        private List<object?>.Enumerator _enumerator;

        internal ResettableEnumerator( List<object?> list )
        {
            this._list = list;
            this._enumerator = list.GetEnumerator();
        }

        /// <inheritdoc />
        public object? Current => this._enumerator.Current;

        /// <inheritdoc />
        public bool MoveNext() => this._enumerator.MoveNext();

        /// <summary>
        /// Resets the enumerator to the beginning of the list.
        /// </summary>
        public void Reset()
        {
            this._enumerator = this._list.GetEnumerator();
        }
    }
}
