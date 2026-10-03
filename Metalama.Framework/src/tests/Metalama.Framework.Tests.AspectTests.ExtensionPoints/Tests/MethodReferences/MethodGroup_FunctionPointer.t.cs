[Redirect]
internal static unsafe class Program
{
  public static void TestMain()
  {
    delegate*<int, int> pointer = &global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.MethodReferences.MethodGroup_FunctionPointer.Interceptors.Compute;
    Console.WriteLine(pointer(1));
  }
}