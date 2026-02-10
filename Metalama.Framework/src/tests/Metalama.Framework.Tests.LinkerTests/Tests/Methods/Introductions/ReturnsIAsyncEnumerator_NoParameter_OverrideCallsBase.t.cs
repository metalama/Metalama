internal class Target
{
  private async IAsyncEnumerator<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Base call replaced with AsyncEnumerableArray<T>.Empty.GetAsyncEnumerator().
    var enumerator = global::Metalama.Framework.RunTime.AsyncEnumerableArray<global::System.Int32>.Empty.GetAsyncEnumerator();
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
