class Target
{
  void Foo(int x)
  {
    Console.WriteLine("Before");
    this.Foo(x, 42);
    Console.WriteLine("After");
  }
  void Foo(int x, int y)
  {
    Console.WriteLine("Original");
  }
}