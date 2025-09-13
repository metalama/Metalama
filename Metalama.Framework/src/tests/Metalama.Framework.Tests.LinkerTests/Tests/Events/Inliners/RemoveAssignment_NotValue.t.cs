public class Target
{
  private EventHandler? _field;
  private event EventHandler Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      this.Foo_Source -= (EventHandler)((s, ea) =>
      {
      });
      Console.WriteLine("After");
    }
  }
  private event EventHandler Foo_Source
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Original");
      this._field -= value;
    }
  }
}