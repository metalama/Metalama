internal class Target
{
  private int _field;
  private int Foo
  {
    set
    {
      Console.WriteLine("Before");
      this.Foo_Source = 42;
      Console.WriteLine("After");
    }
  }
  private int Foo_Source
  {
    set
    {
      Console.WriteLine("Original");
      this._field = value;
    }
  }
}