internal class TargetCode
{
  [Aspect]
  private void NonAsyncMethod()
  {
    object result = null;
    global::System.Console.WriteLine($"result={(object)result}");
    return;
  }
  [Aspect]
  private async void AsyncMethod()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await Task.Yield();
    object result = null;
    global::System.Console.WriteLine($"result={(object)result}");
    return;
  }
}