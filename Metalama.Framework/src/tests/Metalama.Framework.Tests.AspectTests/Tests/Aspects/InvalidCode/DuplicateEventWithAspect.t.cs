// Final Compilation.Emit failed.
// Error CS1061 on `_event`: `'TargetCode' does not contain a definition for '_event' and no accessible extension method '_event' accepting a first argument of type 'TargetCode' could be found (are you missing a using directive or an assembly reference?)`
// Error CS1061 on `_event`: `'TargetCode' does not contain a definition for '_event' and no accessible extension method '_event' accepting a first argument of type 'TargetCode' could be found (are you missing a using directive or an assembly reference?)`
// Error CS0102 on `Event`: `The type 'TargetCode' already contains a definition for 'Event'`
// Error CS0229 on `Event`: `Ambiguity between 'TargetCode.Event' and 'TargetCode.Event'`
// Error CS0229 on `Event`: `Ambiguity between 'TargetCode.Event' and 'TargetCode.Event'`
internal class TargetCode
{
  [Aspect]
  private event EventHandler? Event
  {
    add
    {
      this._event += value;
    }
    remove
    {
      this._event -= value;
    }
  }
  [Aspect]
  private event EventHandler? Event
  {
    add
    {
      global::System.Console.WriteLine("Aspect");
      this.Event += value;
    }
    remove
    {
      global::System.Console.WriteLine("Aspect");
      this.Event -= value;
    }
  }
}