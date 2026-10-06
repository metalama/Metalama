[Redirect]
internal static class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_Reordered.Interceptors.Format(first: Values.Get("a"), second: Values.Get("b")));
}