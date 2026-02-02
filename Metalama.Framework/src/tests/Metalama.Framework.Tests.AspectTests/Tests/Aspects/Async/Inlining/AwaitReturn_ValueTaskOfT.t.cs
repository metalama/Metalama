internal class TargetCode
{
  [Aspect]
  private async ValueTask<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    return a;
  }
}
