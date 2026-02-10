internal class Target
{
  private async IAsyncEnumerable<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Base call replaced with AsyncEnumerableArray<T>.Empty.
    IAsyncEnumerable<int> baseResult = global::Metalama.Framework.RunTime.AsyncEnumerableArray<global::System.Int32>.Empty;
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
