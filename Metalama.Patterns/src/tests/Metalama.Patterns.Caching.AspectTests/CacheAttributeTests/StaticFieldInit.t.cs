using System.Reflection;
using Metalama.Patterns.Caching.Aspects;
#pragma warning disable IDE0052
namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.StaticFieldInit;
[CachingConfiguration(UseDependencyInjection = false)]
public class C
{
  private static readonly int _cachedValue = M();
  private static readonly int _cachedInstanceValue = new C().N();
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
  [Cache]
  public int N()
  {
    static object? Invoke(object? instance, object? [] args)
    {
      return ((C)instance).N_Source();
    }
    return ((ICachingService)CachingService.Default).GetFromCacheOrExecute<int>(_cacheRegistration_N, this, new object[] { }, Invoke);
  }
  private int N_Source() => 10;
  private static readonly CachedMethodMetadata _cacheRegistration_M;
  private static readonly CachedMethodMetadata _cacheRegistration_N;
  static C()
  {
    _cacheRegistration_N = CachedMethodMetadata.Register(typeof(C).GetMethod("N", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) ?? throw new MissingMethodException("The method 'C.N()' could not be found using reflection."), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
    _cacheRegistration_M = CachedMethodMetadata.Register(typeof(C).GetMethod("M", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null) ?? throw new MissingMethodException("The method 'C.M()' could not be found using reflection."), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
  }
}