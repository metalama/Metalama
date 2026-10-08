// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with ArgumentException: The proceed binding gives the parameter index 1 as an argument, which is outside the range of the parameters of the method 'Compute_Interceptor'.`
[Redirect]
internal class Program
{
  public static int Execute() => Source.Compute(21);
}