[Redirect]
internal class Program
{
  public int P { get; } = global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InPropertyAndEventInitializer.Interceptors.Compute(1);
  public event Action E = Handlers.Create(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InPropertyAndEventInitializer.Interceptors.Compute(2));
  public static void TestMain()
  {
    var program = new Program();
    Console.WriteLine(program.P);
    program.E();
  }
}