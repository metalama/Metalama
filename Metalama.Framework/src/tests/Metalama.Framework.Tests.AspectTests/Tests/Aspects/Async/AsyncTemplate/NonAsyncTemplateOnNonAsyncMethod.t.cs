internal class TargetCode
{
  [Aspect]
  public ValueTask<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Getting task");
    global::System.Threading.Tasks.ValueTask<global::System.Int32> task;
    task = new ValueTask<int>(Task.FromResult(a));
    global::System.Console.WriteLine("Got task");
    return (global::System.Threading.Tasks.ValueTask<global::System.Int32>)task;
  }
}