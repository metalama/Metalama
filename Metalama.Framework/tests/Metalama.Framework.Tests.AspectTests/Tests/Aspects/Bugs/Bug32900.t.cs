public partial class TargetClass
{
  [TestAspect]
  public async Task AsyncTaskMethod()
  {
    object result_1 = null;
    try
    {
      await this.AsyncTaskMethod_Source();
    }
    catch
    {
    }
    return;
  }
  private async Task AsyncTaskMethod_Source()
  {
    var result = 42;
    await Task.Yield();
    _ = result;
  }
  [TestAspect]
  public async Task<int> AsyncTaskIntMethod()
  {
    var result_1 = default(global::System.Int32);
    try
    {
      result_1 = (await this.AsyncTaskIntMethod_Source());
    }
    catch
    {
    }
    return (global::System.Int32)result_1;
  }
  private async Task<int> AsyncTaskIntMethod_Source()
  {
    var result = 42;
    await Task.Yield();
    return result;
  }
}