internal class TargetCode
{
  [Aspect1]
  [Aspect2]
  private async void MethodReturningValueTaskOfInt(int a)
  {
    global::System.Console.WriteLine("Aspect1.Before");
    global::System.Console.WriteLine("Aspect2.Before");
    await Task.Yield();
    Console.WriteLine("Oops");
    object result = null;
    global::System.Console.WriteLine("Aspect2.After");
    object result_1 = null;
    global::System.Console.WriteLine("Aspect1.After");
    return;
  }
}
