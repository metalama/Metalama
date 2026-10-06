// Warning TEST0003 on `Shout`: `The redirection of 'StringExtensions.Shout( "static form" )' was refused with ArgumentException: The call site 'StringExtensions.Shout( "static form" )' has no receiver to pass.`
[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_ReducedExtension.Interceptors.Shout("hello"));
    Console.WriteLine(StringExtensions.Shout("static form"));
  }
}