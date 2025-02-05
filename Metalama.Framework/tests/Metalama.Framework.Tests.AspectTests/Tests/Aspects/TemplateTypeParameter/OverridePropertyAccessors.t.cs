public class Target
{
  private int _p;
  [Aspect]
  public int P
  {
    get
    {
      global::System.Console.WriteLine(typeof(global::System.Int32));
      return (global::System.Int32)this._p;
    }
    set
    {
      global::System.Console.WriteLine(typeof(global::System.Int32));
      this._p = value;
    }
  }
}