internal class Target
{
  private async IAsyncEnumerable<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should invoke the empty method (async enumerables still need _Empty stub).
    IAsyncEnumerable<int> baseResult = this.Foo_Empty();
    await foreach (var item in baseResult)
    {
      yield return item;
    }
    Console.WriteLine("After");
  }
  public IAsyncEnumerable<int> Foo()
  {
    return this.Foo_Override();
  }
  private async IAsyncEnumerable<int> Foo_Empty()
  {
    yield break;
  }
}