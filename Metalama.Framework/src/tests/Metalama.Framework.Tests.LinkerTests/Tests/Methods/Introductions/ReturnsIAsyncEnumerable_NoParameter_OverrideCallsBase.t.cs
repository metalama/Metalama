internal class Target
{
  private async IAsyncEnumerable<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should invoke the empty method (async enumerables still need _Empty stub).
    IAsyncEnumerable<int> baseResult = global::System.Linq.AsyncEnumerable.Empty<global::System.Int32>();
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
}
