// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Custom attribute that, when applied to a parameter of a cached method (i.e. a method enhanced by the caching aspect),
/// excludes this parameter from being a part of the cache key.
/// </summary>
[PublicAPI]
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class NotCacheKeyAttribute : Attribute;