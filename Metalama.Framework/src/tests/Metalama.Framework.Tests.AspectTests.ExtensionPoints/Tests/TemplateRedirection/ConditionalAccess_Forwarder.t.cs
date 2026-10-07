[Redirect]
internal class Program
{
  public static void TestMain()
  {
    var text = (Text? )new Text();
    Console.WriteLine(text?.__MetalamaInterceptors_Length__Interceptor());
    text = null;
    Console.WriteLine(text?.__MetalamaInterceptors_Length__Interceptor() ?? -1);
  }
}