[Introduction]
internal class TargetClass
{
  public event EventHandler Event
  {
    add
    {
      value.Invoke(null, new global::System.EventArgs());
      var ev = value;
    }
    remove
    {
      value.Invoke(null, new global::System.EventArgs());
      var ev = value;
    }
  }
}