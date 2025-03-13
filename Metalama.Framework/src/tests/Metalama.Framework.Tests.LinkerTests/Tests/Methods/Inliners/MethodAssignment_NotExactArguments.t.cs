class Target
{
  int Foo(int y, int z)
  {
    Console.WriteLine("Before");
    int x;
    x = this.Foo_Source(z, y);
    Console.WriteLine("After");
    return x;
  }
  private int Foo_Source(int y, int z)
  {
    Console.WriteLine("Original");
    return 42;
  }
}