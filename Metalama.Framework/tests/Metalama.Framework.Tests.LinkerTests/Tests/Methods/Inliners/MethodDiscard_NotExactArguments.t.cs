class Target
{
  int Foo(int x, int y)
  {
    Console.WriteLine("Before");
    _ = this.Foo_Source(y, x);
    Console.WriteLine("After");
    return 42;
  }
  private int Foo_Source(int x, int y)
  {
    Console.WriteLine("Original");
    return 42;
  }
}