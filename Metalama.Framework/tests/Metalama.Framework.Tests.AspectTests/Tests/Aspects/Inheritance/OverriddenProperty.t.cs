internal class Targets
{
  private class BaseClass
  {
    private int _p;
    [Aspect]
    public virtual int P
    {
      get
      {
        return (global::System.Int32)(this._p + 1);
      }
      set
      {
        this._p = value - 1;
      }
    }
  }
  private class DerivedClass : BaseClass
  {
    private int _p;
    public override int P
    {
      get
      {
        return (global::System.Int32)(this._p + 1);
      }
      set
      {
        this._p = value - 1;
      }
    }
  }
}