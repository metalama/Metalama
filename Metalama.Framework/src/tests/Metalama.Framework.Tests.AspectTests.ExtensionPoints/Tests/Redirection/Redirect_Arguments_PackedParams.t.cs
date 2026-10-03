[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_PackedParams.Interceptors.Sum(label: "a", values: [Values.Get(1), Values.Get(2)]));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_PackedParams.Interceptors.Sum(label: "b", values: []));
  }
}