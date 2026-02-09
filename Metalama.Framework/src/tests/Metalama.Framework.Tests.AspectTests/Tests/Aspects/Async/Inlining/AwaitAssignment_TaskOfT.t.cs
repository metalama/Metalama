internal class TargetCode
{
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    global::System.Int32 result;
    await Task.Yield();
    result = a * 2;
    global::System.Console.WriteLine($"After: {(object)result}");
    return (global::System.Int32)result;
  }
}