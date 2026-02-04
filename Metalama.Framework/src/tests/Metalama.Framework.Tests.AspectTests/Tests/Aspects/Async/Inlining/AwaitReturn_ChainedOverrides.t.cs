internal class TargetCode
{
  // Two aspects on same async method - both should inline
  [Aspect1]
  [Aspect2]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Aspect1 Before");
    global::System.Int32 result_1;
    global::System.Console.WriteLine("Aspect2 Before");
    global::System.Int32 result;
    await Task.Yield();
    result = a * 2;
    global::System.Console.WriteLine("Aspect2 After");
    result_1 = (global::System.Int32)result;
    global::System.Console.WriteLine("Aspect1 After");
    return (global::System.Int32)result_1;
  }
}
