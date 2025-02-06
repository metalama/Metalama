// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching;

/// <summary>
/// Custom attribute that, when applied to a parameter of a cached method (i.e. a method enhanced by the caching aspect),
/// excludes this parameter from being a part of the cache key.
/// </summary>
[PublicAPI]
[AttributeUsage( AttributeTargets.Parameter )]
public sealed class NotCacheKeyAttribute : Attribute;