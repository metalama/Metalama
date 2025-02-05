public class Target
{
  private int _p;
  [Aspect]
  public int P
  {
    get
    {
      global::System.Console.WriteLine(5);
      global::System.Console.WriteLine("int");
      return this._p;
    }
    set
    {
      global::System.Console.WriteLine(5);
      global::System.Console.WriteLine("int");
      this._p = value;
    }
  }
}