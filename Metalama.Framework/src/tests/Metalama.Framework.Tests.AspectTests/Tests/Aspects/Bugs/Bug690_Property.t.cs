internal class TargetClass
{
  private int _p1;
  // Without target specifier (should work).
  [MyAspect]
  public int P1
  {
    get
    {
      global::System.Console.WriteLine("Get");
      return this._p1;
    }
    set
    {
      global::System.Console.WriteLine("Set");
      this._p1 = value;
    }
  }
  private int _p2;
  // With explicit property target specifier (should also work).
  [property: MyAspect]
  public int P2
  {
    get
    {
      global::System.Console.WriteLine("Get");
      return this._p2;
    }
    set
    {
      global::System.Console.WriteLine("Set");
      this._p2 = value;
    }
  }
}
