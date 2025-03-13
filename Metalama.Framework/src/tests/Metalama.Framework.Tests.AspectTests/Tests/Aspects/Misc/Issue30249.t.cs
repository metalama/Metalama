internal class C
{
  [MyAspect]
  internal async Task VoidAsyncMethod()
  {
    var result = this.VoidAsyncMethod_Source();
    await result;
    return;
  }
  private async Task VoidAsyncMethod_Source()
  {
    await Task.Yield();
  }
  [MyAspect]
  internal async Task<int> IntAsyncMethod()
  {
    var result = this.IntAsyncMethod_Source();
    return (global::System.Int32)(await result);
  }
  private async Task<int> IntAsyncMethod_Source()
  {
    await Task.Yield();
    return 5;
  }
}