[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var value = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ExplicitTypeArguments.Interceptors.Create<global::System.Int32?>();
    var text = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ExplicitTypeArguments.Interceptors.Create<global::System.String>();
    Console.WriteLine(value == null && text == null);
  }
}