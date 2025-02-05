class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before");
      int x = 0;
      _ = x = this.Foo_Source;
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