// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections;

namespace Metalama.Patterns.Caching.ValueAdapters;

internal sealed class EnumeratorAdapter<T> : ValueAdapter<IEnumerator<T>>
{
    public override object? GetStoredValue( IEnumerator<T>? value )
    {
        if ( value == null )
        {
            return null;
        }

        List<T> list = new();

        while ( value.MoveNext() )
        {
            list.Add( value.Current );
        }

        return list;
    }

    public override IEnumerator<T>? GetExposedValue( object? storedValue ) => storedValue == null ? null : new Enumerator( (List<T>) storedValue );

    private sealed class Enumerator : IEnumerator<T>
    {
        private readonly List<T> _list;
        private int _index = -1;

        public Enumerator( List<T> list )
        {
            this._list = list;
        }

        public T Current
        {
            get
            {
                if ( this._index < 0 || this._list.Count <= this._index )
                {
                    throw new InvalidOperationException();
                }

                return this._list[this._index];
            }
        }

#nullable disable // Duplicate behaviour from IEnumerator<T>.
        object IEnumerator.Current
#nullable restore
            => this.Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            this._index++;

            return this._list.Count > this._index;
        }

        public void Reset() => throw new NotSupportedException( "Cannot reset a cached enumerator." );
    }
}