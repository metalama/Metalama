// Warning TEST0003 on `Print`: `The redirection of 'Source.Print( "a" )' was refused with ArgumentException: The rewritten call binds to 'Interceptors.Print(string)' instead of 'Interceptors.Print(string, params int[])'.`
[Redirect]
internal static class Program
{
  public static void TestMain() => Source.Print("a");
}