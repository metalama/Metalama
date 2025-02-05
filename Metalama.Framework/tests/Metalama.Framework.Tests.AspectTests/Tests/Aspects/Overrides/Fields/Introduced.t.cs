[Introduction]
[Test]
internal class TargetClass : BaseClass
{
  private global::System.Int32 _field;
  public global::System.Int32 Field
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._field;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._field = value;
    }
  }
  private global::System.Int32 _newField;
  public new global::System.Int32 NewField
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return this._newField;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      this._newField = value;
    }
  }
}