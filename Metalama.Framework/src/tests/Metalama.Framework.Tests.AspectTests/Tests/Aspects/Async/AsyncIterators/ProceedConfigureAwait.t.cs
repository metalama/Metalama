class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> Enumerable(int a)
  {
    global::System.Console.WriteLine("Starting Enumerable");
    await foreach (var item in global::System.Threading.Tasks.TaskAsyncEnumerableExtensions.ConfigureAwait(this.Enumerable_Source(a), false))
    {
      global::System.Console.WriteLine($" Intercepting {item}");
      yield return item;
    }
    global::System.Console.WriteLine("Ending Enumerable");
  }
  private async IAsyncEnumerable<int> Enumerable_Source(int a)
  {
    await Task.Yield();
    Console.WriteLine("Yield 1");
    yield return 1;
    Console.WriteLine("Yield 2");
    yield return 2;
    Console.WriteLine("Yield 3");
    yield return 3;
  }
}