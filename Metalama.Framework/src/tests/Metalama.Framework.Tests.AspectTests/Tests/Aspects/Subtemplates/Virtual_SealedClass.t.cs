internal class TargetCode
{
  [Aspect]
  private async Task Method()
  {
    global::System.Console.WriteLine("normal template");
    global::System.Console.WriteLine("virtual method");
    await Task.Yield();
    return;
    throw new global::System.Exception();
  }
}
