class Target
{
  int field;
  int Foo
  {
    set
    {
      Console.WriteLine("Before");
      _ = this.Foo_Source = value;
      Console.WriteLine("After");
    }
  }
  private int Foo_Source
  {
    set
    {
      Console.WriteLine("Original");
      this.field = value;
    }
  }
}