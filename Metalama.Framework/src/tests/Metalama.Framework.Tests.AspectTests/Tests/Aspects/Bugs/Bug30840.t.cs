[TrackedObject]
public struct TrackedClass
{
  private int _i1;
  public int i
  {
    get
    {
      return this._i1;
    }
    set
    {
      this._i1 = value;
      global::System.Console.WriteLine("Overridden setter");
      return;
    }
  }
  public TrackedClass()
  {
  }
}