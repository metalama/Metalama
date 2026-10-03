[Redirect]
internal static class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_ThreeDroppedPacked.Interceptors.FormatLast(fourth: global::Metalama.Framework.RunTime.CallSiteHelper.DropBefore((Values.Get("a"), Values.Get("b"), Values.Get("c")), 4)));
}