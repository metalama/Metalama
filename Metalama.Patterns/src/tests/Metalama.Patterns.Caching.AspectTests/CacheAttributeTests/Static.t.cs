using System.Reflection;
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Aspects.Helpers;
namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.Static;
[CachingConfiguration(UseDependencyInjection = false)]
public class C
{
  [Cache]
  public static int M()
  {
    static object? Invoke(object? instance, object? [] args)
    {
      return M_Source();
    }
    return ((ICachingService)CachingService.Default).GetFromCacheOrExecute<int>(_cacheRegistration_M, null, new object[] { }, Invoke);
  }
  private static int M_Source() => 5;
  private static readonly CachedMethodMetadata _cacheRegistration_M;
  static C()
  {
    _cacheRegistration_M = CachedMethodMetadata.Register(typeof(C).GetMethod("M", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null).ThrowIfMissing("C.M()"), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
  }
}