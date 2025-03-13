[Override]
internal partial class TargetClass
{
  private event EventHandler? _targetEvent1;
  public event EventHandler? TargetEvent1
  {
    add
    {
      global::System.Console.WriteLine("This is the override of TargetEvent1.");
      this._targetEvent1 += value;
      return;
    }
    remove
    {
      global::System.Console.WriteLine("This is the override of TargetEvent1.");
      this._targetEvent1 -= value;
      return;
    }
  }
}