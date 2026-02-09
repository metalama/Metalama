// Warning MY001 on `TargetClass`: `Event 'AccessorEvent': HasRaiseMethod=False, CanRaise=False`
// Warning MY001 on `TargetClass`: `Event 'FieldLikeEvent': HasRaiseMethod=True, CanRaise=True`
// Warning MY001 on `TargetClass`: `Event 'ProgrammaticAccessorEvent': HasRaiseMethod=False, CanRaise=False`
[Introduction]
internal class TargetClass
{
  public event global::System.EventHandler? AccessorEvent
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
  public event global::System.EventHandler? FieldLikeEvent;
  public event global::System.EventHandler ProgrammaticAccessorEvent
  {
    add
    {
      global::System.Console.WriteLine("Add");
    }
    remove
    {
      global::System.Console.WriteLine("Remove");
    }
  }
}
