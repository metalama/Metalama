internal class Target
{
  private async IAsyncEnumerator<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should invoke the empty method (async enumerators still need _Empty stub).
    var enumerator = global::System.Linq.AsyncEnumerable.Empty<global::System.Int32>().GetAsyncEnumerator();
    while (await enumerator.MoveNextAsync())
    {
      yield return enumerator.Current;
    }
    Console.WriteLine("After");
  }
  public IAsyncEnumerator<int> Foo()
  {
    return this.Foo_Override();
  }
}
