class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    int x = 0;
    x += this.Foo_Source();
    Console.WriteLine("After");
    return x;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}