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