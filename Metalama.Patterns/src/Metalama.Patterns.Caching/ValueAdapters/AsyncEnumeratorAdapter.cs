// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETCOREAPP3_0_OR_GREATER
using JetBrains.Annotations;
using Metalama.Framework.RunTime;

namespace Metalama.Patterns.Caching.ValueAdapters;

internal sealed class AsyncEnumeratorAdapter<T> : ValueAdapter<IAsyncEnumerator<T>>
{
    public override bool IsAsyncSupported => true;

    public override async Task<object?> GetStoredValueAsync( IAsyncEnumerator<T>? value, CancellationToken cancellationToken )
    {
        if ( value == null )
        {
            return null;
        }

        return await value.BufferToListAsync( cancellationToken );
    }

    [MustDisposeResource]
    public override IAsyncEnumerator<T>? GetExposedValue( object? storedValue ) => ((AsyncEnumerableList<T>?) storedValue)?.GetAsyncEnumerator();

    public override object GetStoredValue( IAsyncEnumerator<T>? value ) => throw new NotSupportedException();
}

#endif