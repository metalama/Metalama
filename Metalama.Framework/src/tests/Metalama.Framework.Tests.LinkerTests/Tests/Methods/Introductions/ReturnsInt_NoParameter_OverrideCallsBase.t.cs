internal class Target
{
  private int Foo_Override()
  {
    Console.WriteLine("Before");
    // Should return default value instead of calling empty method.
    return default(global::System.Int32);
  }
  public int Foo()
  {
    return this.Foo_Override();
  }
}
