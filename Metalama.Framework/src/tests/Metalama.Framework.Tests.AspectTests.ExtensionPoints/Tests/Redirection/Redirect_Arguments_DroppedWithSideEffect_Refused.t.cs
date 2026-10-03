// Warning TEST0003 on `Format`: `The redirection of 'Source.Format( value.ToUpperInvariant() )' was refused with ArgumentException: The argument 'value.ToUpperInvariant()' of the call site 'Source.Format( value.ToUpperInvariant() )' is not passed, and it can have a side effect, but no argument of the new call can evaluate it in its source order. An adjacent argument must be passed by value.`
[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    var value = "a";
    Console.WriteLine(global::Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Redirection.Redirect_Arguments_DroppedWithSideEffect_Refused.Interceptors.FormatNothing());
    Console.WriteLine(Source.Format(value.ToUpperInvariant()));
  }
}