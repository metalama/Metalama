// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.ValueAdapters;

internal sealed class EnumerableAdapter<T> : ValueAdapter<IEnumerable<T>>
{
    public override object? GetStoredValue( IEnumerable<T>? value )
    {
        if ( value == null )
        {
            return null;
        }

        return new List<T>( value );
    }

    public override IEnumerable<T>? GetExposedValue( object? storedValue ) => (IEnumerable<T>?) storedValue;
}