internal partial class TargetClass
{
  private event EventHandler? _targetEvent3;
  public event EventHandler? TargetEvent3
  {
    add
    {
      global::System.Console.WriteLine("This is the override of TargetEvent3.");
      this._targetEvent3 += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the override of TargetEvent3.");
      this._targetEvent3 -= value;
      return;
    }
  }
}