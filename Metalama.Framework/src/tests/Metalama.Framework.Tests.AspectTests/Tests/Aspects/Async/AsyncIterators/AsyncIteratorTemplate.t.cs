class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> Enumerable(int a)
  {
    global::System.Console.WriteLine("Starting Enumerable");
    await foreach (var item in this.Enumerable_Source(a))
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
  [Aspect]
  public async IAsyncEnumerator<int> Enumerator(int a)
  {
    global::System.Console.WriteLine("Starting Enumerator");
    var enumerator = this.Enumerator_Source(a);
    while (await enumerator.MoveNextAsync())
    {
      global::System.Console.WriteLine($" Intercepting {enumerator.Current}");
      yield return enumerator.Current;
    }
    global::System.Console.WriteLine("Ending Enumerator");
  }
  private async IAsyncEnumerator<int> Enumerator_Source(int a)
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