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
    ProtectedResource? result;
    result = new ProtectedResource();
    await _cachingService.InvalidateAsync(_methodsInvalidatedBy_UpdateProtectedResourceAsync_AE10A3168F93BA6A187A7E438DE50A40[0], this, new object[] { resourceId }, default(CancellationToken));
    return (ProtectedResource? )result;
  }
  [InvalidateCache(nameof(GetResourceNameAsync))]
  public async Task<ProtectedResource?> UpdateProtectedResource2Async(UpdateProtectedResource update, Guid resourceId)
  {
    ProtectedResource? result;
    result = new ProtectedResource();
    await _cachingService.InvalidateAsync(_methodsInvalidatedBy_UpdateProtectedResource2Async_5D88BBAC730DC5F67DE5A9E4107C1BE6[0], this, new object[] { resourceId }, default(CancellationToken));
    return (ProtectedResource? )result;
  }
  private static readonly CachedMethodMetadata _cacheRegistration_GetResourceNameAsync;
  private ICachingService _cachingService;
  private static MethodInfo[] _methodsInvalidatedBy_UpdateProtectedResource2Async_5D88BBAC730DC5F67DE5A9E4107C1BE6;
  private static MethodInfo[] _methodsInvalidatedBy_UpdateProtectedResourceAsync_AE10A3168F93BA6A187A7E438DE50A40;
  static Target()
  {
    _cacheRegistration_GetResourceNameAsync = CachedMethodMetadata.Register(typeof(Target).GetMethod("GetResourceNameAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Guid) }, null) ?? throw new MissingMethodException("The method 'Target.GetResourceNameAsync(Guid)' could not be found using reflection."), new CachedMethodConfiguration() { AbsoluteExpiration = null, AutoReload = null, IgnoreThisParameter = null, Priority = null, ProfileName = (string? )null, SlidingExpiration = null }, true);
    _methodsInvalidatedBy_UpdateProtectedResourceAsync_AE10A3168F93BA6A187A7E438DE50A40 = new MethodInfo[]
    {
      typeof(Target).GetMethod("GetResourceNameAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Guid) }, null) ?? throw new MissingMethodException("The method 'Target.GetResourceNameAsync(Guid)' could not be found using reflection.")
    };
    _methodsInvalidatedBy_UpdateProtectedResource2Async_5D88BBAC730DC5F67DE5A9E4107C1BE6 = new MethodInfo[]
    {
      typeof(Target).GetMethod("GetResourceNameAsync", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Guid) }, null) ?? throw new MissingMethodException("The method 'Target.GetResourceNameAsync(Guid)' could not be found using reflection.")
    };
  }
  public Target(ICachingService? cachingService = null)
  {
    this._cachingService = cachingService ?? throw new System.ArgumentNullException(nameof(cachingService));
  }
}