class Target
{
  void Foo(int x, int y)
  {
    Console.WriteLine("Before");
    this.Foo_Source(y, x);
    Console.WriteLine("After");
  }
  private void Foo_Source(int x, int y)
  {
    Console.WriteLine("Original");
  }
}