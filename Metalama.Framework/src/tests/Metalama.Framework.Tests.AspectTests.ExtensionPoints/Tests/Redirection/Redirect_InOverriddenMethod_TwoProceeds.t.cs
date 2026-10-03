[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(new Program().M());
  private int M()
  {
    this.M_Source();
    return this.M_Source();
  }
  private int M_Source() => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_TwoProceeds.Interceptors.Compute(2);
}