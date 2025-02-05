internal class C
{
  private int _p;
  [TheAspect]
  private int P
  {
    get
    {
      return this._p;
    }
    set
    {
      this._p = value;
    }
  }
}