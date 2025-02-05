class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    return (short)this.Foo_Source();
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}