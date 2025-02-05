class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      return (short)this.Foo_Source;
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