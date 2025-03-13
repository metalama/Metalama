internal class TargetCode
{
  [Aspect]
  private async void MethodReturningVoid(int a)
  {
    global::System.Console.WriteLine("Before");
    await this.MethodReturningVoid_Source(a);
    object result = null;
    global::System.Console.WriteLine("After");
    return;
  }
  private async global::System.Threading.Tasks.ValueTask MethodReturningVoid_Source(int a)
  {
    await Task.Yield();
    Console.WriteLine("Oops");
  }
}