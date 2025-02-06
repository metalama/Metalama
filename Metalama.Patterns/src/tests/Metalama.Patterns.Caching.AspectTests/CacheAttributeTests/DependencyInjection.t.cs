using System.Reflection;
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Aspects.Helpers;
namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.DependencyInjection;
public class C
{
  [Cache]
  public int M()
  {
    static object? Invoke(object? instance, object? [] args)
    {
      return ((C)instance).M_Source();
    }
    return _cachingService.GetFromCacheOrExecute<int>(_cacheRegistration_M, this, new object[] { }, Invoke);
  }
  private int M_Source() => 5;
  private static readonly CachedMethodMetadata _cacheRegistration_M;
  private ICachingService _cachingService;
  static C()
  {
    _cacheRegistration_M = CachedMethodMetadata.Register(typeof(C).GetMethod("M", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null).ThrowIfMissing("C.M()"), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
  }
  public C(ICachingService? cachingService = null)
  {
    this._cachingService = cachingService ?? throw new System.ArgumentNullException(nameof(cachingService));
  }
}