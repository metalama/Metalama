public class TheClass
{
  // C should not be modified.
  public const int C = 4;
  private global::System.Int32 _f;
  // The other fields should be modified.
  public global::System.Int32 F
  {
    get
    {
      global::System.Console.WriteLine("Overridden.");
      return this._f;
    }
    set
    {
      global::System.Console.WriteLine("Overridden.");
      this._f = value;
      return;
    }
  }
  private int _p;
  public int P
  {
    get
    {
      global::System.Console.WriteLine("Overridden.");
      return this._p;
    }
    set
    {
      global::System.Console.WriteLine("Overridden.");
      this._p = value;
      return;
    }
  }
}