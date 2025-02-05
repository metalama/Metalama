[Introduction]
internal class TargetClass
{
  private event global::System.EventHandler IntroducedEvent
  {
    add
    {
      value.Invoke(null, new global::System.EventArgs());
    }
    remove
    {
      value.Invoke(null, new global::System.EventArgs());
    }
  }
}