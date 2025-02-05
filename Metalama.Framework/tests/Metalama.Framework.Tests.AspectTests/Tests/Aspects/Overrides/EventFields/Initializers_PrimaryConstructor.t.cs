[Override]
[Introduction]
internal class TargetClass(EventHandler h)
{
  private event EventHandler? _event = h;
  public event EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._event += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._event -= value;
    }
  }
  private event global::System.EventHandler? _introducedEvent = (global::System.EventHandler? )h;
  public event global::System.EventHandler? IntroducedEvent
  {
    add
    {
      global::System.Console.WriteLine("This is the add template.");
      this._introducedEvent += value;
    }
    remove
    {
      global::System.Console.WriteLine("This is the remove template.");
      this._introducedEvent -= value;
    }
  }
}