using System.Reflection;
using Metalama.Framework.RunTime;
using Metalama.Patterns.Caching.Aspects;
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
    return _cachingService.GetFromCacheOrExecute<int>((CachedMethodMetadata? )_cacheRegistration_M, this, new object[] { }, Invoke);
  }
  private int M_Source() => 5;
  private static readonly CachedMethodMetadata _cacheRegistration_M;
  private ICachingService _cachingService;
  static C()
  {
    _cacheRegistration_M = CachedMethodMetadata.Register(typeof(C).GetMethod("M", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) ?? throw new MissingMethodException("The method 'C.M()' could not be found using reflection."), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
  }
  public C([AspectGenerated] ICachingService? cachingService = null)
  {
    this._cachingService = cachingService ?? throw new System.ArgumentNullException(nameof(cachingService));
  }
}