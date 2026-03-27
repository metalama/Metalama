// Final Compilation.Emit failed.
// Error CS1626 on `yield`: `Cannot yield a value in the body of a try block with a catch clause`
internal class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> AsyncEnumerable(int a)
  {
    try
    {
      var result = (await global::Metalama.Framework.RunTime.RunTimeAspectHelper.BufferAsync(this.AsyncEnumerable_Source(a)));
      global::System.Console.WriteLine("Success AsyncEnumerable");
      await foreach (var r in result)
      {
        yield return r;
      }
      yield break;
    }
    catch (global::System.Exception e)
    {
      global::System.Console.WriteLine($"Caught {e.Message} in AsyncEnumerable");
      throw;
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