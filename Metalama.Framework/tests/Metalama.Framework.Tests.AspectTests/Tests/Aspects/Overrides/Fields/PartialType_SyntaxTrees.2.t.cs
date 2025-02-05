internal partial class TargetClass
{
  private global::System.Int32 _targetField3;
  public global::System.Int32 TargetField3
  {
    get
    {
      global::System.Console.WriteLine("This is the override of TargetField3.");
      return this._targetField3;
    }
    set
    {
      global::System.Console.WriteLine("This is the override of TargetField3.");
      this._targetField3 = value;
      return;
    }
  }
}