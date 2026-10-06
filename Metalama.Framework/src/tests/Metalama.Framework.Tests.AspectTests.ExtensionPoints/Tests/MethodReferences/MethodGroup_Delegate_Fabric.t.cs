internal static class Program
{
  public static void TestMain()
  {
    Func<int, int> converted = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Delegate_Fabric.Interceptors.Compute;
    Console.WriteLine(converted(1));
  }
}