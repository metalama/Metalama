internal class TargetCode
{
  [Aspect]
  private int Method(int a)
  {
    global::System.Console.WriteLine("Overridden.");
    return a;
  }
}