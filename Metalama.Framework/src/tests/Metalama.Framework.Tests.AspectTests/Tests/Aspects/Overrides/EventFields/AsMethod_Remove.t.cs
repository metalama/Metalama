internal class TargetClass
{
  private event EventHandler? _event;
  [Override]
  public event EventHandler? Event
  {
    add
    {
      this._event += value;
    }
    remove
    {
      global::System.Console.WriteLine("Overridden remove");
      this._event -= value;
    }
  }
}