// Warning TEST0003 on `Echo`: `The redirection of 'Source.Echo( 1 )' was refused with InvalidOperationException: The type parameter 'T' of the method 'Echo_Interceptor' cannot be changed, because the extension that created the method builder locks it.`
[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(Source.Echo(1));
}