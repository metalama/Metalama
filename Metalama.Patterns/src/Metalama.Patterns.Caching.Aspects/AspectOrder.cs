// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Patterns.Caching.Aspects;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(CacheKeyAttribute), typeof(ImplementFormattableAspect) )]
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ContractAspect), typeof(CacheAttribute) )]
[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(InvalidateCacheAttribute), typeof(CacheAttribute) )]