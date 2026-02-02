internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    int result = 10;
    result += await this.AsyncMethod_Source(a);
    global::System.Console.WriteLine("After");
    return (global::System.Int32)result;
  }
  private async Task<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
}
