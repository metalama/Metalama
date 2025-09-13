internal class Target
{
  private int Foo
  {
    get
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      return 42;
    }
  }
}