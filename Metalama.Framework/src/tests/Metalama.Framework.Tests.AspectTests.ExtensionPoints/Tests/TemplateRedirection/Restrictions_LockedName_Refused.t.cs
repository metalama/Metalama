// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with InvalidOperationException: The name of the method 'Compute_Interceptor' cannot be changed.`
[Redirect]
internal class Program
{
  public static int Execute() => Source.Compute(21);
}