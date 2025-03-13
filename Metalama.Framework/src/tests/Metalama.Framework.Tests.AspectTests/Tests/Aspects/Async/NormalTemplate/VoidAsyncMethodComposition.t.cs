internal class TargetCode
{
  [Aspect1]
  [Aspect2]
  private async void MethodReturningValueTaskOfInt(int a)
  {
    global::System.Console.WriteLine("Aspect1.Before");
    await this.MethodReturningValueTaskOfInt_Aspect2(a);
    object result_1 = null;
    global::System.Console.WriteLine("Aspect1.After");
    return;
  }
  private async global::System.Threading.Tasks.ValueTask MethodReturningValueTaskOfInt_Source(int a)
  {
    await Task.Yield();
    Console.WriteLine("Oops");
  }
  private async global::System.Threading.Tasks.ValueTask MethodReturningValueTaskOfInt_Aspect2(global::System.Int32 a)
  {
    global::System.Console.WriteLine("Aspect2.Before");
    await this.MethodReturningValueTaskOfInt_Source(a);
    object result = null;
    global::System.Console.WriteLine("Aspect2.After");
    return;
  }
}