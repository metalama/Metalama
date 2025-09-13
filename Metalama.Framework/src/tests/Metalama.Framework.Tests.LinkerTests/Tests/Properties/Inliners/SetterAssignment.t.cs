internal class Target
{
  private int _field;
  private int Foo
  {
    set
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      this._field = value;
      Console.WriteLine("After");
    }
  }
}