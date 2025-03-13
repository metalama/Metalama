class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      return _ = (int)this.Foo_Source;
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