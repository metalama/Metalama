internal class Target
{
  [Log]
  public dynamic DoSomething(dynamic input)
  {
    dynamic result;
    result = input;
    global::System.Console.WriteLine($"Method returned: {(object)result}");
    return (dynamic)result;
  }
  private static void TestMain()
  {
    var target = new Target();
    target.DoSomething("hello");
  }
}
