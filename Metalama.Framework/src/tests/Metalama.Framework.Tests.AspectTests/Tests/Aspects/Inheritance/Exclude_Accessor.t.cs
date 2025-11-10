internal class Targets
{
  private int _a;
  public int A
  {
    get
    {
      global::System.Console.WriteLine("Overridden!");
      return this._a;
    }
    set
    {
      global::System.Console.WriteLine("Overridden!");
      this._a = value;
      return;
    }
  }
  [ExcludeAspect(typeof(Aspect))]
  public int B { get; set; }
  private int _c;
  public int C
  {
    get
    {
      global::System.Console.WriteLine("Overridden!");
      return this._c;
    }
    [ExcludeAspect(typeof(Aspect))]
    set
    {
      this._c = value;
    }
  }
  [ExcludeAspect(typeof(Aspect))]
  public int F;
}