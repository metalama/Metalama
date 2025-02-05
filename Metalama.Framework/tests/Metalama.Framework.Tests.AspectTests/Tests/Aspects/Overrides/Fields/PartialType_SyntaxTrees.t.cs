[Override]
internal partial class TargetClass
{
  private global::System.Int32 _targetField1;
  public global::System.Int32 TargetField1
  {
    get
    {
      global::System.Console.WriteLine("This is the override of TargetField1.");
      return this._targetField1;
    }
    set
    {
      global::System.Console.WriteLine("This is the override of TargetField1.");
      this._targetField1 = value;
      return;
    }
  }
}