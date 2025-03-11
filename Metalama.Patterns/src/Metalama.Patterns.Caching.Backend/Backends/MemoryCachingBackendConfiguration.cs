// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Serializers;
using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Backends;

#pragma warning disable SA1623

[PublicAPI]
public sealed record MemoryCachingBackendConfiguration : CachingBackendConfiguration
{
    /// <summary>
    /// Gets or sets a delegate that receives the object being cached and returns its size, for use in the <see cref="MemoryCacheEntryOptions.Size"/>
    /// property of the <see cref="MemoryCacheEntryOptions"/> object. This allows to enforce a size limit on the <see cref="MemoryCache"/>
    /// (see  <see cref="MemoryCacheOptions.SizeLimit"/>). The default value is a delegate returning the constant 1. This property is ignored
    /// if <see cref="Serializer"/> has a non-null value, because in this case the size is the number of bytes in the serialized item.
    /// </summary>
    public Func<object?, long> SizeCalculator { get; init; } = _ => 1;

    /// <summary>
    /// Gets or sets an optional <see cref="ICachingSerializer"/>. By default, this property is <c>null</c>, and cached objects are referenced
    /// in the <i>as is</i>, without any serialization. Setting this property forces the cache to hold the serialized representation of the cached
    /// object.
    /// </summary>
    /// <remarks>
    /// <para>Using a serializer for the in-memory cache can have two benefits.</para>
    /// <para>First, it allows to test the readiness of the code before switching to a distributed cache like Redis. Running a Redis server on a development
    /// machine is less convenient than running no server, so using an in-memory caching backend with serializer is a good comprise between test
    /// coverage and convenience.</para>
    /// <para>
    /// Second, it allows to cache mutable objects and make sure that every caller receives a fresh instance of the return value of the cached method.
    /// </para>
    /// </remarks>
    public ICachingSerializer? Serializer { get; init; }
}