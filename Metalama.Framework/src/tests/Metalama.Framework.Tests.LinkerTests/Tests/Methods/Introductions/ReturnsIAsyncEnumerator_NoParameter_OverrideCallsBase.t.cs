internal class Target
{
  private async IAsyncEnumerator<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should invoke the empty method (async enumerators still need _Empty stub).
    var enumerator = this.Foo_Empty();
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
  private async IAsyncEnumerator<int> Foo_Empty()
  {
    yield break;
  }
}