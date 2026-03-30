using System.Reflection;
using Metalama.Patterns.Caching.Aspects;
namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.StaticFieldInit;
[CachingConfiguration(UseDependencyInjection = false)]
public class C
{
  private static readonly int _cachedValue = M();
  [Cache]
  public static int M()
  {
    if (_cacheRegistration_M == null)
    {
      return C.M_Source();
    }
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
    _cacheRegistration_M = CachedMethodMetadata.Register(typeof(C).GetMethod("M", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null) ?? throw new MissingMethodException("The method 'C.M()' could not be found using reflection."), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
  }
}