internal class TargetCode
{
  [Aspect]
  private void NonAsyncMethod()
  {
    object result = null;
    global::System.Console.WriteLine($"result={result}");
    return;
  }
  [Aspect]
  private async void AsyncMethod()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await this.AsyncMethod_Source();
    object result = null;
    global::System.Console.WriteLine($"result={result}");
    return;
  }
  private async global::System.Threading.Tasks.ValueTask AsyncMethod_Source()
  {
    await Task.Yield();
  }
}