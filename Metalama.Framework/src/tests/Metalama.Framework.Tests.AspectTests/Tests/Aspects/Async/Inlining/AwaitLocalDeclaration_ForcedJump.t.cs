internal class TargetCode
{
  // Target has early return - forces goto generation during inlining
  [Aspect]
  private async Task<int> AsyncMethod(int a)
  {
    global::System.Console.WriteLine("Before");
    global::System.Int32 result;
    if (a == 0)
    {
      result = 0; // Early return
      goto __aspect_return_1;
    }
    await Task.Yield();
    result = a * 2;
    goto __aspect_return_1;
    __aspect_return_1:
      global::System.Console.WriteLine($"After: {(object)result}");
    return (global::System.Int32)result;
  }
}