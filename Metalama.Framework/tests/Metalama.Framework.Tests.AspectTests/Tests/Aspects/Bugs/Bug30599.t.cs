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
    return (await this.Method_Source(a));
  }
  private async Task<int> Method_Source(int a)
  {
    await Task.Yield();
    return a;
  }
}