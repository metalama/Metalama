internal class Target
{
  [Cache]
  public Task<string?> GetResourceNameAsync(Guid resourceId)
  {
    static async Task<object?> InvokeAsync(object? instance, object? [] args, CancellationToken cancellationToken)
    {
      return await ((Target)instance).GetResourceNameAsync_Source((Guid)args[0]);
    }
    return _cachingService.GetFromCacheOrExecuteTaskAsync<string?>(_cacheRegistration_GetResourceNameAsync, this, (object? [])new object[] { resourceId }, InvokeAsync, null, default);
  }
  private async Task<string?> GetResourceNameAsync_Source(Guid resourceId)
  {
    return "42";
  }
  [InvalidateCache(nameof(GetResourceNameAsync))]
  public async Task<ProtectedResource?> UpdateProtectedResourceAsync(Guid resourceId, UpdateProtectedResource update)
  {
    var result = await UpdateProtectedResourceAsync_Source(resourceId, update);
    await _cachingService.InvalidateAsync(_methodsInvalidatedBy_UpdateProtectedResourceAsync_AE10A3168F93BA6A187A7E438DE50A40[0], this, new object[] { resourceId }, default(CancellationToken));
    return result;
  }
  private async Task<ProtectedResource?> UpdateProtectedResourceAsync_Source(Guid resourceId, UpdateProtectedResource update)
  {
    return new ProtectedResource();
  }
  [InvalidateCache(nameof(GetResourceNameAsync))]
  public async Task<ProtectedResource?> UpdateProtectedResource2Async(UpdateProtectedResource update, Guid resourceId)
  {
    var result = await UpdateProtectedResource2Async_Source(update, resourceId);
    await _cachingService.InvalidateAsync(_methodsInvalidatedBy_UpdateProtectedResource2Async_5D88BBAC730DC5F67DE5A9E4107C1BE6[0], this, new object[] { resourceId }, default(CancellationToken));
    return result;
  }
  private async Task<ProtectedResource?> UpdateProtectedResource2Async_Source(UpdateProtectedResource update, Guid resourceId)
  {
    return new ProtectedResource();
  }
  private static readonly CachedMethodMetadata _cacheRegistration_GetResourceNameAsync;
  private ICachingService _cachingService;
  private static MethodInfo[] _methodsInvalidatedBy_UpdateProtectedResource2Async_5D88BBAC730DC5F67DE5A9E4107C1BE6;
  private static MethodInfo[] _methodsInvalidatedBy_UpdateProtectedResourceAsync_AE10A3168F93BA6A187A7E438DE50A40;
  static Target()
  {
    _cacheRegistration_GetResourceNameAsync = CachedMethodMetadata.Register(typeof(Target).GetMethod("GetResourceNameAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Guid) }, null).ThrowIfMissing("Target.GetResourceNameAsync(Guid)"), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, true);
    _methodsInvalidatedBy_UpdateProtectedResourceAsync_AE10A3168F93BA6A187A7E438DE50A40 = new MethodInfo[]
    {
      typeof(Target).GetMethod("GetResourceNameAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Guid) }, null).ThrowIfMissing("Target.GetResourceNameAsync(Guid)")
    };
    _methodsInvalidatedBy_UpdateProtectedResource2Async_5D88BBAC730DC5F67DE5A9E4107C1BE6 = new MethodInfo[]
    {
      typeof(Target).GetMethod("GetResourceNameAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Guid) }, null).ThrowIfMissing("Target.GetResourceNameAsync(Guid)")
    };
  }
  public Target(ICachingService? cachingService = null)
  {
    this._cachingService = cachingService ?? throw new System.ArgumentNullException(nameof(cachingService));
  }
}