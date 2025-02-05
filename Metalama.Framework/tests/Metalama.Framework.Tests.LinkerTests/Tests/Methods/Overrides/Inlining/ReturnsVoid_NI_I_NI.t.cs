class Target
{
  void Foo()
  {
    this.Foo_Override2();
  }
  private void Foo_Source()
  {
    Console.WriteLine("Original");
  }
  void Foo_Override2()
  {
    Console.WriteLine("Before2");
    Console.WriteLine("Before1");
    this.Foo_Source();
    Console.WriteLine("After1");
    Console.WriteLine("After2");
  }
}