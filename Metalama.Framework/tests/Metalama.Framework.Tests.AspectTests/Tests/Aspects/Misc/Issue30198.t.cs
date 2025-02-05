internal class Target
{
  [Aspect]
  private void M(int x)
  {
    global::System.Console.WriteLine("Hello, world.");
    global::System.Console.WriteLine("Param Target.M(int)/x");
    var n = "ExcludeLoggingAttribute";
    _ = x;
    return;
  }
}