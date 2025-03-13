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