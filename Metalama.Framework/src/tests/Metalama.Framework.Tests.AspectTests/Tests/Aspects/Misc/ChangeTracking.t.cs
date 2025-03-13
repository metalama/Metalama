[ChangeTrackingAspect]
internal class MyClass
{
  private int _a;
  public int A
  {
    get
    {
      return this._a;
    }
    set
    {
      this._isASpecified = true;
      this._a = value;
    }
  }
  private string? _b;
  public string? B
  {
    get
    {
      return this._b;
    }
    set
    {
      this._isBSpecified = true;
      this._b = value;
    }
  }
  public global::System.Boolean _isASpecified { get; private set; }
  public global::System.Boolean _isBSpecified { get; private set; }
}