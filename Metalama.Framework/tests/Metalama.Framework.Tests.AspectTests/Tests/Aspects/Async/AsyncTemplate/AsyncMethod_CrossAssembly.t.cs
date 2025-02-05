class TargetCode
{
  [Aspect]
  int NormalMethod(int a)
  {
    return a;
  }
  [Aspect]
  async Task<int> AsyncTaskResultMethod(int a)
  {
    await global::System.Threading.Tasks.Task.Yield();
    var result = await this.AsyncTaskResultMethod_Source(a);
    global::System.Console.WriteLine($"result={result}");
    return (global::System.Int32)result;
  }
  private async Task<int> AsyncTaskResultMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
  [Aspect]
  async Task AsyncTaskMethod()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await this.AsyncTaskMethod_Source();
    object result = null;
    global::System.Console.WriteLine($"result={result}");
    return;
  }
  private async Task AsyncTaskMethod_Source()
  {
    await Task.Yield();
  }
}