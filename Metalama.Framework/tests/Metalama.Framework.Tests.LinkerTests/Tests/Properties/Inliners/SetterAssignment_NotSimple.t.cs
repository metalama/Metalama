class Target
{
  int field;
  int Foo
  {
    get
    {
      return this.Foo_Source;
    }
    set
    {
      Console.WriteLine("Before");
      this.Foo_Source += value;
      Console.WriteLine("After");
    }
  }
  private int Foo_Source
  {
    get
    {
      return 0;
    }
    set
    {
      Console.WriteLine("Original");
      this.field = value;
    }
  }
}