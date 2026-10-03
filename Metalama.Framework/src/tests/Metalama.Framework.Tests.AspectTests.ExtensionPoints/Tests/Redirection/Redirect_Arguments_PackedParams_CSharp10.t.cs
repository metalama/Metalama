[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_PackedParams_CSharp10.Interceptors.Sum(label: "a", values: new global::System.Int32[] { 1, 2 }));
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_PackedParams_CSharp10.Interceptors.Sum(label: "b", values: global::System.Array.Empty<global::System.Int32>()));
  }
}