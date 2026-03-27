internal class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> AsyncEnumerable(int a)
  {
    try
    {
      var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumerable_Source(a)));
      await foreach (var r in result)
      {
        yield return r;
      }
      yield break;
    }
    finally
    {
      global::System.Console.WriteLine("Finally AsyncEnumerable");
    }
  }
  private async IAsyncEnumerable<int> AsyncEnumerable_Source(int a)
  {
    Console.WriteLine("Yield 1");
    yield return 1;
    await Task.Yield();
    Console.WriteLine("Yield 2");
    yield return 2;
  }
}