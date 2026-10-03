[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_After.Interceptors.FormatFirst(first: global::Metalama.Framework.RunTime.CallSiteHelper.DropAfter(Values.Get("a"), Values.Get("b"))));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_After.Interceptors.FormatFirst(first: Values.Get("c")));
  }
}