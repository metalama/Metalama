[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var counter = new Counter();
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_RefReceiver.Interceptors.Increment(ref counter);
    Console.WriteLine(counter.Value);
  }
}