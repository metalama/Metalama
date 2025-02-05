public class Target
{
  private int _test;
  [TestAspect]
  public int Test
  {
    get
    {
      global::System.Console.WriteLine("getter");
      return this._test;
    }
    private set
    {
      global::System.Console.WriteLine("setter");
      this._test = value;
    }
  }
}