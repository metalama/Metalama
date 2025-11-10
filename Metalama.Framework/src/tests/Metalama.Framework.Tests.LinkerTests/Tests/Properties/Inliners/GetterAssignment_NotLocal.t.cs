internal class Target
{
  private int x;
  private int Foo
  {
    get
    {
      Console.WriteLine("Before");
      this.x = this.Foo_Source;
      Console.WriteLine("After");
      return this.x;
    }
  }
  private int Foo_Source
  {
    get
    {
      Console.WriteLine("Original");
      return 42;
    }
  }
}