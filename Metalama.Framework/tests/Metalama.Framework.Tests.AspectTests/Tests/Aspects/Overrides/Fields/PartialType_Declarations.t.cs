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
internal partial class TargetClass
{
  private global::System.Int32 _targetField2;
  public global::System.Int32 TargetField2
  {
    get
    {
      global::System.Console.WriteLine("This is the override of TargetField2.");
      return this._targetField2;
    }
    set
    {
      global::System.Console.WriteLine("This is the override of TargetField2.");
      this._targetField2 = value;
      return;
    }
  }
}
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