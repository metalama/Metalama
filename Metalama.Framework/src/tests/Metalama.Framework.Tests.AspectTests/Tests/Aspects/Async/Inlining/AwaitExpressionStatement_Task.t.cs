internal class TargetCode
{
  [Aspect]
  private async Task AsyncMethod()
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    Console.WriteLine("Original");
    global::System.Console.WriteLine("After");
    return;
  }
}
