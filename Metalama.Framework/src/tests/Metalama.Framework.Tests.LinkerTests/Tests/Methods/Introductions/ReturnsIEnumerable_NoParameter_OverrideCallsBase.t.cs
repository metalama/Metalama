internal class Target
{
  private IEnumerable<int> Foo_Override()
  {
    Console.WriteLine("Before");
    // Should invoke the empty method (enumerables still need _Empty stub).
    foreach (var item in global::System.Linq.Enumerable.Empty<global::System.Int32>())
    {
      yield return item;
    }
    Console.WriteLine("After");
  }
  public IEnumerable<int> Foo()
  {
    return this.Foo_Override();
  }
}
