class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      long x = this.Foo_Source;
      Console.WriteLine("After");
      return (int)x;
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