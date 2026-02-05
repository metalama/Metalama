public partial class TargetClass
{
  [TestAspect]
  public async Task AsyncTaskMethod()
  {
    object result_1 = null;
    try
    {
      var result = 42;
      await Task.Yield();
      _ = result;
    }
    catch
    {
    }
    return;
  }
  [TestAspect]
  public async Task<int> AsyncTaskIntMethod()
  {
    var result_1 = default(global::System.Int32);
    try
    {
      var result = 42;
      await Task.Yield();
      result_1 = result;
    }
    catch
    {
    }
    return (global::System.Int32)result_1;
  }
}
