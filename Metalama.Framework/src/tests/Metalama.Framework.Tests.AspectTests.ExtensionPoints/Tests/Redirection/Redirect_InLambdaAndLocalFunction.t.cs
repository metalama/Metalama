[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Func<int> lambda = () => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InLambdaAndLocalFunction.Interceptors.Compute(1);
    int LocalFunction() => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InLambdaAndLocalFunction.Interceptors.Compute(2);
    Console.WriteLine($"{lambda()} {LocalFunction()}");
  }
}