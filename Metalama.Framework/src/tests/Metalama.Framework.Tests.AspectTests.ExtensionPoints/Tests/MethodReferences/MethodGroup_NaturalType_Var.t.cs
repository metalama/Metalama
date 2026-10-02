[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var inferred = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_NaturalType_Var.Interceptors.Compute;
    Console.WriteLine(inferred(1));
  }
}