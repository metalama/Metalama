internal class Target
{
  public int Foo()
  {
    return this.Foo_Override();
  }
  private int Foo_Override()
  {
    Console.WriteLine("Before");
    // Should return default value instead of calling empty method.
    return default(int);
  }
}
