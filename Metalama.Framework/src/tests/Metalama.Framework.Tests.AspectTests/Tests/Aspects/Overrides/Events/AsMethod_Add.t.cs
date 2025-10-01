internal class TargetClass
{
  private EventHandler? _handler;
  [Override]
  public event EventHandler Event
  {
    add
    {
      global::System.Console.WriteLine("Overridden add");
      this._handler += value;
      Console.WriteLine("Original add");
    }
    remove
    {
      this._handler -= value;
      Console.WriteLine("Original remove");
    }
  }
}