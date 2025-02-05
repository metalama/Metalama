public partial class TestClass
{
  [LoggingAspect]
  public async Task<T?> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
  {
    global::System.Console.WriteLine("Executing TestClass.GetAsync<T>(CacheKey, Func<Task<T>>)");
    return (await this.GetAsync_Source<T>(key, acquire));
  }
  private async Task<T?> GetAsync_Source<T>(CacheKey key, Func<Task<T>> acquire)
  {
    await Task.Yield();
    return default;
  }
  [LoggingAspect]
  public async Task<T?> GetAsync<T>(CacheKey key, Func<T> acquire)
  {
    global::System.Console.WriteLine("Executing TestClass.GetAsync<T>(CacheKey, Func<T>)");
    return (await this.GetAsync_Source<T>(key, acquire));
  }
  private async Task<T?> GetAsync_Source<T>(CacheKey key, Func<T> acquire)
  {
    await Task.Yield();
    return default;
  }
  [LoggingAspect]
  public T? Get<T>(CacheKey key, Func<Task<T>> acquire)
  {
    global::System.Console.WriteLine("Executing TestClass.Get<T>(CacheKey, Func<Task<T>>)");
    return default;
  }
  [LoggingAspect]
  public T? Get<T>(CacheKey key, Func<T> acquire)
  {
    global::System.Console.WriteLine("Executing TestClass.Get<T>(CacheKey, Func<T>)");
    return default;
  }
  [LoggingAspect]
  public IEnumerable<T?> GetEnumerable<T>(CacheKey key, Func<Task<T>> acquire)
  {
    global::System.Console.WriteLine("Executing TestClass.GetEnumerable<T>(CacheKey, Func<Task<T>>)");
    return global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.GetEnumerable_Source<T>(key, acquire));
  }
  private IEnumerable<T?> GetEnumerable_Source<T>(CacheKey key, Func<Task<T>> acquire)
  {
    yield return default;
  }
  [LoggingAspect]
  public IEnumerable<T?> GetEnumerable<T>(CacheKey key, Func<T> acquire)
  {
    global::System.Console.WriteLine("Executing TestClass.GetEnumerable<T>(CacheKey, Func<T>)");
    return global::Metalama.Framework.RunTime.RunTimeAspectHelper.Buffer(this.GetEnumerable_Source<T>(key, acquire));
  }
  private IEnumerable<T?> GetEnumerable_Source<T>(CacheKey key, Func<T> acquire)
  {
    yield return default;
  }
}