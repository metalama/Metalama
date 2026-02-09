internal class TargetCode
{
  [Aspect]
  private int NormalMethod(int a)
  {
    return a;
  }
  [Aspect]
  private async Task<int> AsyncTaskResultMethod(int a)
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Int32 result;
    await Task.Yield();
    result = a;
    global::System.Console.WriteLine($"result={(object)result}");
    return (global::System.Int32)result;
  }
  [Aspect]
  private async Task AsyncTaskMethod()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await Task.Yield();
    object result = null;
    global::System.Console.WriteLine($"result={(object)result}");
    return;
  }
}