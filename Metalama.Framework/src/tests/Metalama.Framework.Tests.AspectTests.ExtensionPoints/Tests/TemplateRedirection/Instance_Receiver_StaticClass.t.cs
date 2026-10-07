[Redirect]
internal class Program
{
  public static void TestMain()
  {
    var greeter = new Greeter();
    Console.WriteLine(global::MetalamaInterceptors.Greet_Interceptor(greeter, "world"));
  }
}