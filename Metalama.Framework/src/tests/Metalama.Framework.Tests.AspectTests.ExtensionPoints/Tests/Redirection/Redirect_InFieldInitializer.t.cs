[Redirect]
internal class Program
{
  private int _a = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer.Interceptors.Compute(1), _b = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer.Interceptors.Compute(2);
  private static readonly int _c = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InFieldInitializer.Interceptors.Compute(3);
  public static void TestMain()
  {
    var program = new Program();
    Console.WriteLine($"{program._a} {program._b} {_c}");
  }
}