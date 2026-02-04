internal class TargetCode
{
  [Aspect]
  private async void MethodReturningVoid(int a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    Console.WriteLine("Oops");
    object result = null;
    global::System.Console.WriteLine("After");
    return;
  }
}
