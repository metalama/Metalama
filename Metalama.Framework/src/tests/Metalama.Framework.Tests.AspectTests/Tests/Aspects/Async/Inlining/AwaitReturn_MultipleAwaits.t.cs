internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    await global::System.Threading.Tasks.Task.Yield();
    await Task.Yield();
    return a;
  }
}
