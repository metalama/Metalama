// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with InvalidOperationException: The test restrictions refuse the type 'System.Object' for the parameter 'x'.`
[Redirect]
internal class Program
{
  public static void TestMain() => Console.WriteLine(Source.Compute(21));
}