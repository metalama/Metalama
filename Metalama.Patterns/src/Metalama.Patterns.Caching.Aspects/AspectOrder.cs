// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Patterns.Caching.Aspects;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(CacheKeyAttribute), typeof(ImplementFormattableAspect) )]
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ContractAspect), typeof(CacheAttribute) )]
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvalidateCacheAttribute), typeof(CacheAttribute) )]