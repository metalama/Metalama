internal class Target : ITarget
{
  private readonly string _p = null !;
  public string P
  {
    get
    {
      var returnValue = this._p;
      if (returnValue == null)
      {
        throw new global::System.ArgumentNullException();
      }
      return returnValue;
    }
    private init
    {
      this._p = value;
    }
  }
  public Target()
  {
    P = "42";
  }
}