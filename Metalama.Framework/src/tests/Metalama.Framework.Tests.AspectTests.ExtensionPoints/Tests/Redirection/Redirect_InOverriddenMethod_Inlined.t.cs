[Redirect]
internal class Program
{
  public static void TestMain() => new Program().M();
  private void M()
  {
    global::System.Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_Inlined.Source.Compute(1));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_Inlined.Interceptors.Compute(2));
    return;
  }
}