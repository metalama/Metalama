// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.TypeExtensions;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.ValueAdapters;

internal sealed class ValueAdapterFactory
{
    private readonly ConcurrentDictionary<Type, TypeExtensionInfo<IValueAdapter>> _valueAdaptersByValueType = new();
    private readonly TypeExtensionFactory<IValueAdapter> _factory;

    internal ValueAdapterFactory( TypeExtensionFactory<IValueAdapter> factory )
    {
        this._factory = factory;
    }

    /// <summary>
    /// Gets an <see cref="IValueAdapter"/> given a value type.
    /// </summary>
    /// <param name="valueType">The type of the cached value (typically the return type of the cached method).</param>
    /// <returns>A value adapter for <paramref name="valueType"/>, or <c>null</c> if no value adapter is available for <paramref name="valueType"/>.</returns>
    public IValueAdapter? Get( Type valueType ) => this._valueAdaptersByValueType.GetOrAdd( valueType, this.GetCore ).Extension;

    private TypeExtensionInfo<IValueAdapter> GetCore( Type valueType ) => this._factory.GetTypeExtension( valueType, this.CacheUpdateCallback );

    private void CacheUpdateCallback( TypeExtensionInfo<IValueAdapter> typeExtension )
        => this._valueAdaptersByValueType[typeExtension.ObjectType] = typeExtension;
}