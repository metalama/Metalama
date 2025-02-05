[Introduction]
internal class TargetClass
{
  private global::System.Int32 _value;
  public global::System.Int32 Value
  {
    get
    {
      return this._value;
    }
    set
    {
      value = 42;
      this._value = value;
    }
  }
}