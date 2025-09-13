internal class Target
{
  private int Foo()
  {
    Console.WriteLine("Before");
    var x = 0;
    x = x = this.Foo_Source();
    Console.WriteLine("After");
    return x;
  }
  private int Foo_Source()
  {
    Console.WriteLine("Original");
    return 42;
  }
}