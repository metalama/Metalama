using System.Reflection;
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
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