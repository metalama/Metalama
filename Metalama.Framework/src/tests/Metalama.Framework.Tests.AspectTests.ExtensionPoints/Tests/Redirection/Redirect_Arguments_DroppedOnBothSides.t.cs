[Redirect]
internal static class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedOnBothSides.Interceptors.FormatMiddle(second: global::Metalama.Framework.RunTime.CallSiteHelper.DropBefore(Values.Get("a"), global::Metalama.Framework.RunTime.CallSiteHelper.DropAfter(Values.Get("b"), Values.Get("c")))));
}