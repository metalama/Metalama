[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_After.Interceptors.FormatFirst(first: Values.Get("a") switch
    {
      var __value1 => Values.Get("b") switch
      {
        _ => __value1
      }}));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_After.Interceptors.FormatFirst(first: Values.Get("c")));
  }
}