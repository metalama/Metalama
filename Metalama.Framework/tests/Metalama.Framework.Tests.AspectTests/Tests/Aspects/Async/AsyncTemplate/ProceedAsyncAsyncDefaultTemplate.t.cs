internal class TargetCode
{
  [Aspect]
  private async Task AsyncTaskMethod()
  {
    await this.AsyncTaskMethod_Source();
  }
  private async Task AsyncTaskMethod_Source()
  {
    await Task.Yield();
  }
}