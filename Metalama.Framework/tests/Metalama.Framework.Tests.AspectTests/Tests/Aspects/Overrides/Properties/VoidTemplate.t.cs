internal class TargetClass
{
  private int _p;
  [Override]
  private int P
  {
    get
    {
      var value = this._p;
      return (global::System.Int32)(value == null ? default : value);
    }
    set
    {
      this._p = value;
    }
  }
}