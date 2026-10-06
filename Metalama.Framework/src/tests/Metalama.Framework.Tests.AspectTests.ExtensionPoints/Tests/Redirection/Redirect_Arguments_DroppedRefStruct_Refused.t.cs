// Warning TEST0003 on `Format`: `The redirection of 'Source.Format( "a", Token.Create() )' was refused with ArgumentException: The argument 'Token.Create()' of the call site 'Source.Format( "a", Token.Create() )' is not passed, and it can have a side effect, but it cannot be evaluated separately: its type 'Token' is a ref struct or a pointer type, which cannot be a type argument.`
[Redirect]
internal static class Program
{
  public static void TestMain() => Console.WriteLine(Source.Format("a", Token.Create()));
}