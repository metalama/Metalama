[Introduction]
internal class TargetClass
{
  private event EventHandler? _event;
  public event EventHandler? Event
  {
    add
    {
      value.Invoke(null, new global::System.EventArgs());
      this._event += value;
    }
    remove
    {
      value.Invoke(null, new global::System.EventArgs());
      this._event -= value;
    }
  }
}