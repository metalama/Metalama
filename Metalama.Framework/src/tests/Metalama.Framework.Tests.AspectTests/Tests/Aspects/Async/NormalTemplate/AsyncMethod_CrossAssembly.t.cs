internal class TargetCode
{
  [Aspect]
  private async Task<int> MethodReturningTaskOfInt(int a)
  {
    global::System.Console.WriteLine("Before");
    global::System.Int32 result;
    await Task.Yield();
    result = a;
    global::System.Console.WriteLine("After");
    return (global::System.Int32)result;
  }
  [Aspect]
  private async Task MethodReturningTask(int a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    Console.WriteLine("Oops");
    object result = null;
    global::System.Console.WriteLine("After");
    return;
  }
  [Aspect]
  private async ValueTask<int> MethodReturningValueTaskOfInt(int a)
  {
    global::System.Console.WriteLine("Before");
    global::System.Int32 result;
    await Task.Yield();
    result = a;
    global::System.Console.WriteLine("After");
    return (global::System.Int32)result;
  }
  [Aspect]
  private async ValueTask MethodReturningValueTask(int a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    Console.WriteLine("Oops");
    object result = null;
    global::System.Console.WriteLine("After");
    return;
  }
}
