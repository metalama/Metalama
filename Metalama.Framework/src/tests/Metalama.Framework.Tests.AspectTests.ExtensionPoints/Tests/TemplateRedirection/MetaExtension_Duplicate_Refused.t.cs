// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with ArgumentException: Two meta extensions of the template have the same type.`
[Redirect]
internal class Program
{
  public static int Execute() => Source.Compute(21);
}