[Redirect]
internal class Program : Greeter
{
  public static void TestMain()
  {
    var greeter = new Greeter();
    global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReceiverAsFirstArgument.Interceptors.Greet(greeter, "explicit");
    new Program
    {
      Name = "program"
    }.Run();
  }
  private void Run() => global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReceiverAsFirstArgument.Interceptors.Greet(this, "implicit");
}