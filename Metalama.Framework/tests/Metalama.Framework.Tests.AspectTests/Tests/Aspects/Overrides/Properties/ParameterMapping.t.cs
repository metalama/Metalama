[Introduction]
internal class TargetClass
{
  private int _value;
  public int Value
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