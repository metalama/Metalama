internal class TargetCode
{
  [Aspect1]
  [Aspect2]
  private async void AsyncMethod()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await global::System.Threading.Tasks.Task.Yield();
    await Task.Yield();
    object result = null;
    global::System.Console.WriteLine($"result={(object)result}");
    object result_1 = null;
    global::System.Console.WriteLine($"result={(object)result_1}");
    return;
  }
}