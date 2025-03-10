// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Services;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Advising;

internal sealed class ObjectReaderFactory : IGlobalService, IDisposable
{
    private readonly WeakCache<Type, ObjectReaderTypeAdapter> _types = new();

    public IObjectReader GetReader( in ProjectServiceProvider serviceProvider, object? instance )
        => instance switch
        {
            null => ObjectReader.Empty,
            IObjectReader objectReader => objectReader,
            IReadOnlyDictionary<string, object?> dictionary => new ObjectReaderDictionaryWrapper( dictionary ),
            _ => new ObjectReader( instance, this, serviceProvider )
        };

    public IObjectReader GetLazyReader( ProjectServiceProvider serviceProvider, object? instance1, Func<object?> getInstance2 )
        => new LazyObjectReader(
            new Lazy<IObjectReader>(
                () =>
                {
                    var instance2 = getInstance2();

                    return (instance1, instance2) switch
                    {
                        (not null, null) => this.GetReader( serviceProvider, instance1 ),
                        (null, not null) => this.GetReader( serviceProvider, instance2 ),
                        _ => new ObjectReaderMergeWrapper( this.GetReader( serviceProvider, instance2 ), this.GetReader( serviceProvider, instance1 ) )
                    };
                } ) );

    internal ObjectReaderTypeAdapter GetTypeAdapter( Type type ) => this._types.GetOrAdd( type, t => new ObjectReaderTypeAdapter( t ) );

    public void Dispose() => this._types.Dispose();
}