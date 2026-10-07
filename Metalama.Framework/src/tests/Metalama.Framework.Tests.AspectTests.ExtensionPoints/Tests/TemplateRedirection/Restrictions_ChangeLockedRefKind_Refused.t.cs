// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with InvalidOperationException: The reference kind of the parameter 'x' of the method 'Compute_Interceptor' cannot be changed, because the extension that created the method builder locks it.`
[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(Source.Compute(21));
}