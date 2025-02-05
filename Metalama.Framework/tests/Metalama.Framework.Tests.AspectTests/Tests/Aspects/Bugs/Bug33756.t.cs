public class SelfCachedClass
{
  [Cache]
  public int Add(int a, int b)
  {
    {
      var cacheKey = $"Add({string.Join(", ", new object[] { a, b })})";
      if (!_cache.TryGetValue(cacheKey, out var returnValue))
      {
        returnValue = a + b;
        _cache.TryAdd(cacheKey, returnValue);
      }
      return (global::System.Int32)returnValue;
    }
    return default;
  }
  [CacheAndRetry]
  public int Rmove(int a, int b)
  {
    {
      var cacheKey = $"Rmove({string.Join(", ", new object[] { a, b })})";
      if (!_cache.TryGetValue(cacheKey, out var returnValue))
      {
        returnValue = a - b;
        _cache.TryAdd(cacheKey, returnValue);
      }
      return (global::System.Int32)returnValue;
    }
    return default;
  }
  private global::System.Collections.Concurrent.ConcurrentDictionary<global::System.String, global::System.Object?> _cache = (global::System.Collections.Concurrent.ConcurrentDictionary<global::System.String, global::System.Object?>)(new());
}