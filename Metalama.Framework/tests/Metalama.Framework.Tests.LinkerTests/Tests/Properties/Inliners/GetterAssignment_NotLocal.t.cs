class Target
{
  int x;
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      x = this.Foo_Source;
      Console.WriteLine("After");
      return x;
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