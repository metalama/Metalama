// Warning TEST0003 on `Echo`: `The redirection of 'Source.Echo( 1 )' was refused with InvalidOperationException: The name of the type parameter 'T' of the method 'Echo_Interceptor' cannot be changed.`
[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(Source.Echo(1));
}