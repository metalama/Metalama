[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var value = 5;
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Cast.Interceptors.Describe(value: (global::System.Int64)(value)));
  }
}