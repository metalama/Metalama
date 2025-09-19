internal class Target
{
  private int Foo()
  {
    Console.WriteLine("Before");
    return this.Foo2();
  }
  private short Foo2()
  {
    Console.WriteLine("Original");
    return 42;
  }
}