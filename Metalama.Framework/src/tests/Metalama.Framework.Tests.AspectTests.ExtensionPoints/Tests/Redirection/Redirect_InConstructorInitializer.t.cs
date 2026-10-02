[Redirect]
internal class Program : Base
{
  public Program() : base(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InConstructorInitializer.Interceptors.Compute(1))
  {
  }
  public static void TestMain() => Console.WriteLine(new Program().Value);
}