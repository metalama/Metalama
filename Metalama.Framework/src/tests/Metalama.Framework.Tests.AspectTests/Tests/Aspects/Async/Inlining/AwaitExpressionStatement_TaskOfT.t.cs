internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    _ = (global::System.Int32)(a * 2);
    global::System.Console.WriteLine("After");
    return default;
  }
}
