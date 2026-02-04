internal class TargetCode
{
  public bool _disposed;
  [Aspect]
  private async Task<int> Method(int a)
  {
    if ((bool)this._disposed)
    {
      throw new global::System.InvalidOperationException("The object has already been disposed");
    }
    await Task.Yield();
    return a;
  }
}
