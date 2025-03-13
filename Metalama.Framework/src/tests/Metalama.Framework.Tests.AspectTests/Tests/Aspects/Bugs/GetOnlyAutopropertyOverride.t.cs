public class Target
{
  private readonly int _test;
  [TestAspect]
  public int Test
  {
    get
    {
      global::System.Console.WriteLine("getter");
      return this._test;
    }
    private init
    {
      global::System.Console.WriteLine("setter");
      this._test = value;
    }
  }
}