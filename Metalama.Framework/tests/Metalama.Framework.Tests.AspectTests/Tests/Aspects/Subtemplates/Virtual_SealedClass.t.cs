internal class TargetCode
{
  [Aspect]
  private async Task Method()
  {
    global::System.Console.WriteLine("normal template");
    global::System.Console.WriteLine("virtual method");
    await this.Method_Source();
    return;
    throw new global::System.Exception();
  }
  private async Task Method_Source()
  {
    await Task.Yield();
  }
}