internal class TargetCode
{
  [Aspect]
  private async Task<string?> AsyncMethod(string? a)
  {
    global::System.Console.WriteLine("Before");
    await Task.Yield();
    return a;
  }
}
