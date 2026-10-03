[Redirect]
internal class Program
{
  [global::System.ObsoleteAttribute]
  private int _a = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer_SplitDeclarators.Interceptors.Compute(1);
  private int _b = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer_SplitDeclarators.Interceptors.Compute(2);
#pragma warning disable CS0612
  public static void TestMain() => Console.WriteLine(new Program()._a + new Program()._b);
#pragma warning restore CS0612
}