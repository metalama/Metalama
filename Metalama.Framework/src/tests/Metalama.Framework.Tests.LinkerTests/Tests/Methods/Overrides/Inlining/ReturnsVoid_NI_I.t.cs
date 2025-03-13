class Target
{
  void Foo()
  {
    this.Foo_Override();
  }
  void Foo_Override()
  {
    Console.WriteLine("Before");
    Console.WriteLine("Original");
    Console.WriteLine("After");
  }
}