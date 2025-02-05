class Target
{
  void Foo()
  {
    Console.WriteLine("Before2");
    this.Foo_Override1();
    Console.WriteLine("After2");
  }
  void Foo_Override1()
  {
    Console.WriteLine("Before1");
    Console.WriteLine("Original");
    Console.WriteLine("After1");
  }
}