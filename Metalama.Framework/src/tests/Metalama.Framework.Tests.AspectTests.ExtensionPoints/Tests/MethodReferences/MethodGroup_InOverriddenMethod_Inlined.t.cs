[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(new Program().M()(2));
  private Func<int, int> M()
  {
    return global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_InOverriddenMethod_Inlined.Interceptors.Compute;
  }
}