internal class TestClass
{
  [TestAspect]
  private async void Execute(bool param)
  {
    try
    {
      await this.Execute_Source(param);
      return;
    }
    catch (global::System.Exception)when (param)
    {
      return;
    }
  }
  private async global::System.Threading.Tasks.ValueTask Execute_Source(bool param)
  {
    await Task.CompletedTask;
  }
}