class TargetCode
{
  [Aspect]
  public async IAsyncEnumerable<int> EnumerableWithoutAsync(int a)
  {
    await foreach (var r in this.EnumerableWithoutAsync_Source(a))
    {
      yield return r;
    }
    yield break;
  }
  private IAsyncEnumerable<int> EnumerableWithoutAsync_Source(int a) => Enumerable(a);
  public async IAsyncEnumerable<int> Enumerable(int a)
  {
    await Task.Yield();
    Console.WriteLine("Yield 1");
    yield return 1;
  }
  [Aspect]
  public async IAsyncEnumerator<int> EnumeratorWithoutAsync(int a)
  {
    await using (var enumerator = this.EnumeratorWithoutAsync_Source(a))
    {
      while (await enumerator.MoveNextAsync())
      {
        yield return enumerator.Current;
      }
    }
    yield break;
  }
  private IAsyncEnumerator<int> EnumeratorWithoutAsync_Source(int a) => Enumerator(a);
  public async IAsyncEnumerator<int> Enumerator(int a)
  {
    await Task.Yield();
    Console.WriteLine("Yield 1");
    yield return 1;
  }
}