[Redirect]
internal class Program
{
  private readonly Func<int, int> _function = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_InFieldInitializer.Interceptors.Compute;
  public static void TestMain() => Console.WriteLine(new Program()._function(1));
}