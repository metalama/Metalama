[Redirect]
internal class Program
{
  public static void TestMain()
  {
    Console.WriteLine(global::MetalamaInterceptors.Compute_Interceptor(1));
    Console.WriteLine(global::MetalamaInterceptors.Compute_Interceptor1(2));
  }
}