// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with ArgumentException: Cannot use the template 'RedirectAttribute.Intercept(int, int)' to implement the method 'MetalamaInterceptors.Compute_Interceptor(int)': the method does not contain a parameter 'y'. Available parameters are: 'x'.`
[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(Source.Compute(21));
}