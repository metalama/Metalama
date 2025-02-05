internal class TargetCode
{
  [Aspect]
  public ValueTask<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Getting task");
    var task = this.AsyncMethod_Source(a);
    global::System.Console.WriteLine("Got task");
    return (global::System.Threading.Tasks.ValueTask<global::System.Int32>)task;
  }
  private async ValueTask<int> AsyncMethod_Source(int a)
  {
    await Task.Yield();
    return a;
  }
}