[TestAspect]
public partial class Imp : IBase
{
  private int _x;
  int IBase.X
  {
    get
    {
      return this._x;
    }
    set
    {
      if (value != this._x)
      {
        this._x = value;
      }
    }
  }
}