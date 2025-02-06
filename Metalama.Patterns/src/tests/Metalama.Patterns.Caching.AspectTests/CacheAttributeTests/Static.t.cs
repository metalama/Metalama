using System.Reflection;
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.
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