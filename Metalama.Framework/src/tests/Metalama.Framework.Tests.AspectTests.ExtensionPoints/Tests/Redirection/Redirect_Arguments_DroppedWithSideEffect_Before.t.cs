[Redirect]
internal static class Program
{
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_Before.Interceptors.FormatLast(third: Values.Get("a") switch
  {
    _ => Values.Get("b") switch
    {
      _ => 3
    }}));
}