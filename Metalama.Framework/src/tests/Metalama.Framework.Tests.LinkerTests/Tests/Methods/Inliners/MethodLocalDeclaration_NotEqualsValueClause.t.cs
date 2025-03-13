class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    var(x, y) = (0, this.Foo_Source());
    Console.WriteLine("After");
    return x;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}