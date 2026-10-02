// Warning TEST0003 on `Format`: `The redirection of 'Source.Format( "a", second.ToUpperInvariant() )' was refused with ArgumentException: The argument 'second.ToUpperInvariant()' of the call site 'Source.Format( "a", second.ToUpperInvariant() )' is not passed, but it can have a side effect or it is passed by reference.`
[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var second = "b";
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_Refused.Interceptors.FormatFirst(first: "a"));
    Console.WriteLine(Source.Format("a", second.ToUpperInvariant()));
  }
}