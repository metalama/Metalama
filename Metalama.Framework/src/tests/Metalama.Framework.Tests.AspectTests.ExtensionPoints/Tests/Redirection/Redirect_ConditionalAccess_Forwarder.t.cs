[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Greeter? missing = null;
    var greeter = new Greeter();
    Console.WriteLine(missing?.__Metalama_Framework_Tests_AspectTests_ExtensionPoints_Tests_Redirection_Redirect__ConditionalAccess__Forwarder_Interceptors_InterceptedHello("a") ?? "null");
    Console.WriteLine(greeter?.__Metalama_Framework_Tests_AspectTests_ExtensionPoints_Tests_Redirection_Redirect__ConditionalAccess__Forwarder_Interceptors_InterceptedHello("b") ?? "null");
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ConditionalAccess_Forwarder.Interceptors.InterceptedHello(new Greeter(), "c"));
  }
}