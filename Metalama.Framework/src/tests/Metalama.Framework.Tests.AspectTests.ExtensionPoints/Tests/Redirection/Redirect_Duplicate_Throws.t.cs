// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 1 )' was refused with InvalidOperationException: A redirection was already requested for 'Source.Compute( 1 )'.`
[Redirect]
internal static class Program
{
  [RedirectMethod]
  public static void TestMain() => Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Duplicate_Throws.Interceptors.Compute(1));
}