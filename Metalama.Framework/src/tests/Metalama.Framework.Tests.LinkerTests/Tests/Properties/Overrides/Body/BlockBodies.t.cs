internal class Target
{
  private int Foo
  {
    get
    {
      Console.WriteLine("Get");
      return 42;
    }
    set
    {
      Console.WriteLine("Set");
    }
  }
}