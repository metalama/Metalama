internal class Target
{
  [Log]
  public int Add(int a, int b)
  {
    global::System.Console.WriteLine($"  a = {a}");
    global::System.Console.WriteLine($"  b = {b}");
    return a + b;
  }
  [Log]
  public dynamic Process(dynamic input)
  {
    global::System.Console.WriteLine($"  input = {(object)input}");
    return input;
  }
  private static void TestMain()
  {
    var target = new Target();
    target.Add(1, 2);
    target.Process("hello");
  }
}