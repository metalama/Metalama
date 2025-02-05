internal partial class TargetClass
{
  private event EventHandler? _targetEvent2;
  public event EventHandler? TargetEvent2
  {
    add
    {
      global::System.Console.WriteLine("This is the override of TargetEvent2.");
      this._targetEvent2 += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the override of TargetEvent2.");
      this._targetEvent2 -= value;
      return;
    }
  }
}