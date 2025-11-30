// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

/// <summary>
/// Interface for compile-time classifiers that determine how method parameters should be handled
/// in cache key generation.
/// </summary>
/// <remarks>
/// <para>Implementations of this interface are registered through the
/// <see cref="CachingOptionsBuilder.AddParameterClassifier"/> method and are evaluated at compile time
/// for each parameter of cached methods.</para>
/// <para>Classifiers can exclude parameters from the cache key (for example, to exclude <c>ILogger</c>
/// parameters) or report parameters as ineligible for caching (for example, parameters of types
/// that cannot be reliably serialized to a cache key).</para>
/// </remarks>
/// <seealso cref="CacheParameterClassification"/>
/// <seealso cref="CachingOptionsBuilder.AddParameterClassifier"/>
/// <seealso href="@caching-exclude-parameters"/>
public interface ICacheParameterClassifier : ICompileTimeSerializable
{
    /// <summary>
    /// Evaluates a method parameter and returns a classification indicating how it should be handled
    /// in cache key generation.
    /// </summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <returns>A <see cref="CacheParameterClassification"/> value indicating how the parameter should be handled.</returns>
    CacheParameterClassification GetClassification( IParameter parameter );
}