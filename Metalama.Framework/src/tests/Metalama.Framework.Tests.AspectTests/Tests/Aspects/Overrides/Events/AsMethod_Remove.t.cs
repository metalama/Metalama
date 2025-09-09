internal class TargetClass
{
  private EventHandler? _handler;
  [Override]
  public event EventHandler Event
  {
    add
    {
      this._handler += value;
      Console.WriteLine("Original add");
    }
    remove
    {
      global::System.Console.WriteLine("Overridden invoke");
      this._handler -= value;
      Console.WriteLine("Original remove");
    }
  }
}