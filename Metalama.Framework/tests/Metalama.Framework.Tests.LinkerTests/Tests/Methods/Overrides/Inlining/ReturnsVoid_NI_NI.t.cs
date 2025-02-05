class Target
{
  void Foo()
  {
    this.Foo_Override();
  }
  private void Foo_Source()
  {
    Console.WriteLine("Original");
  }
  void Foo_Override()
  {
    Console.WriteLine("Before");
    this.Foo_Source();
    Console.WriteLine("After");
  }
}