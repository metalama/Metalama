internal class TargetClass
{
  private event EventHandler? _event;
  [Override]
  public event EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("Overridden add");
      this._event += value;
    }
    remove
    {
      this._event -= value;
    }
  }
}