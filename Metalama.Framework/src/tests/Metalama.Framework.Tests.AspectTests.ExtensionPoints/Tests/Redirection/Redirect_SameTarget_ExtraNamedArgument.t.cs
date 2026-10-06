[Redirect]
internal static class Program
{
  public static void TestMain() => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_SameTarget_ExtraNamedArgument.Logger.Log("message", origin: "redirected");
}