public class Target
{
  private EventHandler? _field;
  public event EventHandler? Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      this._field -= value;
      Console.WriteLine("After");
    }
  }
}