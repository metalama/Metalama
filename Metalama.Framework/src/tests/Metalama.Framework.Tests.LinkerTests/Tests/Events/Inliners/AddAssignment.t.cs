public class Target
{
  private EventHandler? _field;
  private event EventHandler Foo
  {
    add
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      this._field += value;
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}