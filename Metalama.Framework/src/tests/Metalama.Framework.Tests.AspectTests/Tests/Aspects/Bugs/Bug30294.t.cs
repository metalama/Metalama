internal class TestClass
{
  [TestAspect]
  private async void Execute(bool param)
  {
    try
    {
      await Task.CompletedTask;
      return;
    }
    catch (global::System.Exception)when (param)
    {
      return;
    }
  }
}
