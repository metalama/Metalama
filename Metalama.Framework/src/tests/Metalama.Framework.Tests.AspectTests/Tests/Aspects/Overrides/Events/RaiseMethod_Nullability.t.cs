// Warning MY001 on `FieldLikeEvent`: `Event 'FieldLikeEvent': HasRaiseMethod=True, CanRaise=True`
// Warning MY001 on `AccessorEvent`: `Event 'AccessorEvent': HasRaiseMethod=False, CanRaise=False`
// Warning MY001 on `AbstractEvent`: `Event 'AbstractEvent': HasRaiseMethod=False, CanRaise=False`
// Warning MY001 on `InterfaceEvent`: `Event 'InterfaceEvent': HasRaiseMethod=False, CanRaise=False`
internal class TargetClass
{
  [CheckRaiseMethod]
  public event EventHandler? FieldLikeEvent;
  [CheckRaiseMethod]
  public event EventHandler AccessorEvent
  {
    add
    {
    }
    remove
    {
    }
  }
}
