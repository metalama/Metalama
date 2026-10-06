[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Func<int, int> converted = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Delegate.Interceptors.Compute;
    var created = new Func<int, int>(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_Delegate.Interceptors.Compute);
    Console.WriteLine($"{converted(1)} {created(2)} {Source.Compute(3)}");
  }
}