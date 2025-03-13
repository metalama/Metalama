internal class TargetCode
{
  [Aspect1]
  [Aspect2]
  private async void AsyncMethod()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await this.AsyncMethod_Aspect2();
    object result_1 = null;
    global::System.Console.WriteLine($"result={result_1}");
    return;
  }
  private async global::System.Threading.Tasks.ValueTask AsyncMethod_Source()
  {
    await Task.Yield();
  }
  private async global::System.Threading.Tasks.ValueTask AsyncMethod_Aspect2()
  {
    await global::System.Threading.Tasks.Task.Yield();
    await this.AsyncMethod_Source();
    object result = null;
    global::System.Console.WriteLine($"result={result}");
    return;
  }
}