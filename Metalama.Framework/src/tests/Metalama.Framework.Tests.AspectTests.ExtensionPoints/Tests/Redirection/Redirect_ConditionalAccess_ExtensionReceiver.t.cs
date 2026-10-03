[Redirect]
internal static class Program
{
  public static void TestMain()
  {
    Greeter? missing = null;
    var greeter = new Greeter();
    Console.WriteLine(missing?.InterceptedHello("a") ?? "null");
    Console.WriteLine(greeter?.InterceptedHello("b") ?? "null");
    Console.WriteLine(new Greeter().InterceptedHello("c"));
  }
}