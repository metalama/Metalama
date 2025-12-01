// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Caching.Aspects;

/// <summary>
/// Aspect that, when applied to a field or property, includes that member in the cache key generation
/// for the declaring type when instances are used as parameters of cached methods.
/// </summary>
/// <remarks>
/// <para>When a cached method has a parameter of a custom type, Metalama Caching needs to generate a unique
/// string to represent the value in the cache key. The <see cref="CacheKeyAttribute"/> aspect automatically
/// implements the <see cref="Flashtrace.Formatters.IFormattable{T}"/> interface for the
/// <see cref="Metalama.Patterns.Caching.Formatters.CacheKeyFormatting"/> role, including all marked
/// fields and properties in the generated cache key.</para>
/// <para>As a best practice, include the full type name in cache keys to avoid collisions between different types
/// that share the same identifying properties.</para>
/// </remarks>
/// <example>
/// <code>
/// public class Product
/// {
///     [CacheKey]
///     public int Id { get; }
///
///     [CacheKey]
///     public string Category { get; }
///
///     public string Description { get; } // Not part of cache key
/// }
/// </code>
/// </example>
/// <seealso cref="CacheAttribute"/>
/// <seealso cref="NotCacheKeyAttribute"/>
/// <seealso href="@caching-keys"/>
public sealed class CacheKeyAttribute : FieldOrPropertyAspect
{
    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        builder.Outbound.Select( f => f.DeclaringType ).RequireAspect<ImplementFormattableAspect>();
    }
}