internal class TargetCode
{
  [Aspect]
  private string Method2(string s)
  {
    global::System.Console.WriteLine("Hello, world.");
    return s;
  }
}