using System.Reflection;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Aspects.Configuration;
using Metalama.Patterns.Caching.Aspects.Helpers;
namespace Metalama.Patterns.Caching.AspectTests.CacheAttributeTests.ParameterFilter;
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class TheCacheParameterClassifier : ICacheParameterClassifier
{
  public CacheParameterClassification GetClassification(IParameter parameter) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class Fabric : ProjectFabric
{
  public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
internal class TheClass
{
  [Cache]
  // y should be marked with [NotCacheKey].
  public int CachedMethod(int x, [NotCacheKey] IDisposable y)
  {
    static object? Invoke(object? instance, object? [] args)
    {
      return ((TheClass)instance).CachedMethod_Source((int)args[0], (IDisposable)args[1]);
    }
    return _cachingService.GetFromCacheOrExecute<int>(_cacheRegistration_CachedMethod, this, new object[] { x, y }, Invoke);
  }
  private int CachedMethod_Source(int x, IDisposable y)
  {
    return x;
  }
  private static readonly CachedMethodMetadata _cacheRegistration_CachedMethod;
  private ICachingService _cachingService;
  static TheClass()
  {
    _cacheRegistration_CachedMethod = CachedMethodMetadata.Register(typeof(TheClass).GetMethod("CachedMethod", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), typeof(IDisposable) }, null).ThrowIfMissing("TheClass.CachedMethod(int, IDisposable)"), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, false);
  }
  public TheClass(ICachingService? cachingService = null)
  {
    this._cachingService = cachingService ?? throw new System.ArgumentNullException(nameof(cachingService));
  }
}