// Warning TEST0003 on `Greet`: `The redirection of 'new Greeter().Greet( "x" )' was refused with ArgumentException: The receiver of 'new Greeter().Greet( "x" )' must be passed, because the method is not static. Use another receiver mode or pass RedirectedArgument.SourceReceiver.`
[Redirect]
internal static class Program
{
  public static void TestMain() => new Greeter().Greet("x");
}