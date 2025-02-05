class Target
{
  void Foo()
  {
    Console.WriteLine("Before");
    this.Foo_Source();
    Console.WriteLine("After");
  }
  private void Foo_Source()
  {
    Console.WriteLine("Original");
  }
}