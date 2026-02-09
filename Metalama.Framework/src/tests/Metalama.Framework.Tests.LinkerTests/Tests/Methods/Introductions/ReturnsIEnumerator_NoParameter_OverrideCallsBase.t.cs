internal class Target
{
  private IEnumerator<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should invoke the empty method (enumerators still need _Empty stub).
    var enumerator = global::System.Linq.Enumerable.Empty<global::System.Int32>().GetEnumerator();
    while (enumerator.MoveNext())
    {
      yield return enumerator.Current;
    }
    Console.WriteLine("After");
  }
  public IEnumerator<int> Foo()
  {
    return this.Foo_Override();
  }
}
