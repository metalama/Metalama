internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    var condition = global::System.DateTime.Now.Ticks % 2 == 0;
    return (global::System.Int32)(condition ? await this.AsyncMethod_Source(a) : 0);
  }
  private async Task<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
}
