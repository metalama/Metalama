internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    return a * 2;
  }
}
