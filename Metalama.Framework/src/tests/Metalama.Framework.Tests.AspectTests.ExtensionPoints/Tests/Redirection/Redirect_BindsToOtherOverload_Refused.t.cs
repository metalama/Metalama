// Warning TEST0003 on `Print`: `The redirection of 'Source.Print( "a" )' was refused with ArgumentException: The call site 'Source.Print( "a" )' cannot be redirected to 'Interceptors.Print(string, int)', because it binds to 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_BindsToOtherOverload_Refused.Interceptors.Print(string)' when it is written as 'global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_BindsToOtherOverload_Refused.Interceptors.Print( "a" )'.`
[Redirect]
internal static class Program
{
  public static void TestMain() => Source.Print("a");
}