class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    long x = this.Foo_Source();
    Console.WriteLine("After");
    return (int)x;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}