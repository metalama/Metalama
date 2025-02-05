internal class TargetCode
{
  [Aspect]
  private async Task<int> MethodReturningTaskOfInt(int a)
  {
    global::System.Console.WriteLine("Before");
    var result = (await this.MethodReturningTaskOfInt_Source(a));
    global::System.Console.WriteLine("After");
    return (global::System.Int32)result;
  }
  private async Task<int> MethodReturningTaskOfInt_Source(int a)
  {
    await Task.Yield();
    return a;
  }
  [Aspect]
  async Task MethodReturningTask(int a)
  {
    global::System.Console.WriteLine("Before");
    await this.MethodReturningTask_Source(a);
    object result = null;
    global::System.Console.WriteLine("After");
    return;
  }
  private async Task MethodReturningTask_Source(int a)
  {
    await Task.Yield();
    Console.WriteLine("Oops");
  }
  [Aspect]
  private async ValueTask<int> MethodReturningValueTaskOfInt(int a)
  {
    global::System.Console.WriteLine("Before");
    var result = (await this.MethodReturningValueTaskOfInt_Source(a));
    global::System.Console.WriteLine("After");
    return (global::System.Int32)result;
  }
  private async ValueTask<int> MethodReturningValueTaskOfInt_Source(int a)
  {
    await Task.Yield();
    return a;
  }
  [Aspect]
  async ValueTask MethodReturningValueTask(int a)
  {
    global::System.Console.WriteLine("Before");
    await this.MethodReturningValueTask_Source(a);
    object result = null;
    global::System.Console.WriteLine("After");
    return;
  }
  private async ValueTask MethodReturningValueTask_Source(int a)
  {
    await Task.Yield();
    Console.WriteLine("Oops");
  }
}