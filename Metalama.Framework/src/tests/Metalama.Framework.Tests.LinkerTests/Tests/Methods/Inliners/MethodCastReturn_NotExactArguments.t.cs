class Target
{
  int Foo(int x, int y)
  {
    Console.WriteLine("Before");
    return (int)this.Foo_Source(y, x);
  }
  private int Foo_Source(int x, int y)
  {
    Console.WriteLine("Original");
    return 42;
  }
}