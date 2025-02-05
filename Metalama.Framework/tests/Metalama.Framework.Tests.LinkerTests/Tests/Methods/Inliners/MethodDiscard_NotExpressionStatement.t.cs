class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    _ = _ = this.Foo_Source();
    Console.WriteLine("After");
    return 42;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}