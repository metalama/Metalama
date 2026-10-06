[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Expression<Func<int, int>> expression = x => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InExpressionTree.Interceptors.Compute(x);
    Console.WriteLine(expression.Compile()(1));
  }
}