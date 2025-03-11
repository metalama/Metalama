// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETCOREAPP3_0_OR_GREATER
using Metalama.Framework.RunTime;

namespace Metalama.Patterns.Caching.ValueAdapters;

internal sealed class AsyncEnumerableAdapter<T> : ValueAdapter<IAsyncEnumerable<T>>
{
    public override bool IsAsyncSupported => true;

    public override async Task<object?> GetStoredValueAsync( IAsyncEnumerable<T>? value, CancellationToken cancellationToken )
    {
        if ( value == null )
        {
            return null;
        }

        return await value.BufferAsync( cancellationToken );
    }

    public override IAsyncEnumerable<T>? GetExposedValue( object? storedValue ) => (IAsyncEnumerable<T>?) storedValue;

    public override object GetStoredValue( IAsyncEnumerable<T>? value ) => throw new NotSupportedException();
}

#endif