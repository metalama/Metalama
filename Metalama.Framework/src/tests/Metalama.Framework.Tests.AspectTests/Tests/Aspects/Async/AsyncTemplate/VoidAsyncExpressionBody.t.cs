internal class TargetCode
{
  // Regression test for #1638: an expression-bodied, void-returning async Task method
  // generated CS0030 "Cannot convert type 'void' to 'System.Threading.Tasks.Task'".
  [Aspect]
  private async Task DoVoidAsync()
  {
    global::System.Console.WriteLine("enter");
    await Task.Delay(1);
    return;
  }
  [Aspect]
  private async ValueTask DoVoidValueTaskAsync()
  {
    global::System.Console.WriteLine("enter");
    await new ValueTask(Task.Delay(1));
    return;
  }
  [Aspect]
  private async Task<int> GetValueAsync()
  {
    global::System.Console.WriteLine("enter");
    return await Task.FromResult(42);
  }
}