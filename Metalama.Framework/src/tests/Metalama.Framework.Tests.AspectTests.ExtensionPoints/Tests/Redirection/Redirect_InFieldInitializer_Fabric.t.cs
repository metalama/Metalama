internal class Program
{
  private int _a = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer_Fabric.Interceptors.Compute(1);
  public static void TestMain() => Console.WriteLine(new Program()._a);
}