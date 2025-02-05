internal class TargetCode
{
  // The normal template should be applied because YieldAwaitable does not have a method builder.
  [Aspect1]
  [Aspect2]
  public async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Async intercept");
    await global::System.Threading.Tasks.Task.Yield();
    var result = await this.AsyncMethod_Aspect2(a);
    return (global::System.Int32)result;
  }
  private async Task<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
  private global::System.Threading.Tasks.Task<global::System.Int32> AsyncMethod_Aspect2(global::System.Int32 a)
  {
    global::System.Console.WriteLine("Non-async intercept");
    return this.AsyncMethod_Source(a);
  }
  [Aspect1]
  [Aspect2]
  public async Task<int> NonAsyncMethod(int a)
  {
    global::System.Console.WriteLine("Async intercept");
    await global::System.Threading.Tasks.Task.Yield();
    var result = await this.NonAsyncMethod_Aspect2(a);
    return (global::System.Int32)result;
  }
  private global::System.Threading.Tasks.Task<global::System.Int32> NonAsyncMethod_Aspect2(global::System.Int32 a)
  {
    global::System.Console.WriteLine("Non-async intercept");
    return Task.FromResult(a);
  }
}