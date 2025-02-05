class Target
{
  int Foo(int x, int y)
  {
    Console.WriteLine("Before");
    int z = this.Foo_Source(y, x);
    Console.WriteLine("After");
    return z;
  }
  private int Foo_Source(int x, int y)
  {
    Console.WriteLine("Original");
    return 42;
  }
}