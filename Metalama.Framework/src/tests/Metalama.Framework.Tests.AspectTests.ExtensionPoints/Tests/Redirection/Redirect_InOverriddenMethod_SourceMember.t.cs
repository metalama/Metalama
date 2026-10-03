[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(new Program().M());
  private string M()
  {
    string Proceed() => this.M_Source();
    return (global::System.String)Proceed();
  }
  private string M_Source() => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_InOverriddenMethod_SourceMember.Interceptors.Where("x", member: "M");
}