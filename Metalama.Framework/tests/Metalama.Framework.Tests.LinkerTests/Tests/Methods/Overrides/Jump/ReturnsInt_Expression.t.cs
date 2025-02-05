class Target
{
  int Foo(int x)
  {
    return this.Foo_Source(x);
  }
  private int Foo_Source(int x)
  {
    Console.WriteLine("Original Start");
    if (x == 0)
    {
      return 42;
    }
    Console.WriteLine("Original End");
    return x;
  }
}