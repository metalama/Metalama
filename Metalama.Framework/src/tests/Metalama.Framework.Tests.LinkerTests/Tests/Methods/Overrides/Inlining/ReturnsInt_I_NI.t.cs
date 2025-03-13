class Target
{
  int Foo()
  {
    Console.WriteLine("Before");
    int result;
    result = this.Foo_Source();
    Console.WriteLine("After");
    return result;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}