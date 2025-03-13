internal class Program
{
  [Log]
  private static void MyMethod(string who)
  {
    global::System.Console.WriteLine("Entering " + $"MyMethod( who = {who} )");
    try
    {
      // Some very typical business code.
      Console.WriteLine($"Hello, {who}!");
      return;
    }
    finally
    {
      global::System.Console.WriteLine("Leaving " + $"MyMethod( who = {who} )");
    }
  }
  private static void TestMain()
  {
    MyMethod("Lama");
  }
}