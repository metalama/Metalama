[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedAfterValueWithoutNaturalType.Interceptors.FormatFirst(first: global::Metalama.Framework.RunTime.CallSiteHelper.DropAfter<global::System.Collections.Generic.List<global::System.Int32>?, global::System.String>(default, Values.Get("x"))));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedAfterValueWithoutNaturalType.Interceptors.FormatFirst(first: global::Metalama.Framework.RunTime.CallSiteHelper.DropAfter<global::System.Collections.Generic.List<global::System.Int32>?, global::System.String>([1, 2], Values.Get("y"))));
  }
}