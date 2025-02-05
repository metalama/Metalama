internal class TargetCode
{
  [Aspect1]
  [Aspect2]
  public Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Non-async intercept");
    return this.AsyncMethod_Aspect2(a);
  }
  private async Task<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
  private async global::System.Threading.Tasks.Task<global::System.Int32> AsyncMethod_Aspect2(global::System.Int32 a)
  {
    global::System.Console.WriteLine("Async intercept");
    await global::System.Threading.Tasks.Task.Yield();
    var result = await this.AsyncMethod_Source(a);
    return (global::System.Int32)result;
  }
  [Aspect1]
  [Aspect2]
  public Task<int> NonAsyncMethod(int a)
  {
    global::System.Console.WriteLine("Non-async intercept");
    return this.NonAsyncMethod_Aspect2(a);
  }
  private Task<int> NonAsyncMethod_Source(int a)
  {
    return Task.FromResult(a);
  }
  private async global::System.Threading.Tasks.Task<global::System.Int32> NonAsyncMethod_Aspect2(global::System.Int32 a)
  {
    global::System.Console.WriteLine("Async intercept");
    await global::System.Threading.Tasks.Task.Yield();
    var result = await this.NonAsyncMethod_Source(a);
    return (global::System.Int32)result;
  }
}