internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    return (global::System.Int32)(await this.AsyncMethod_Source(a) + 1);
  }
  private async Task<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
}
