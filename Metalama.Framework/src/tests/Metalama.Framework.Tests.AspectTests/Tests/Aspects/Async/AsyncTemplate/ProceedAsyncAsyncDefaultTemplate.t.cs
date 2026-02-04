internal class TargetCode
{
  [Aspect]
  private async Task AsyncTaskMethod()
  {
    await Task.Yield();
  }
}
