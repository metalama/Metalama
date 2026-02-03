internal class TargetCode
{
  [Aspect]
  private async Task<string?> AsyncMethod(string? a)
  {
    global::System.Console.WriteLine("Before");
    return (global::System.String? )(await this.AsyncMethod_Source(a) ?? "default");
  }
  private async Task<string?> AsyncMethod_Source(string? a)
  {
    await Task.Yield();
    return a;
  }
}
