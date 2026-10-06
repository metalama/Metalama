[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var vector = new Vector(3, 4);
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InReceiver.Interceptors.LengthSquared(in vector));
  }
}