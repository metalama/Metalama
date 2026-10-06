[Redirect]
internal class Program
{
  private int _p = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenPropertyInitializer.Interceptors.Compute(1);
  public int P
  {
    get
    {
      return this._p;
    }
    set
    {
      this._p = value;
    }
  }
  public static void TestMain() => Console.WriteLine(new Program().P);
}