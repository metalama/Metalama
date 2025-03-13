internal class TargetCode
{
  [ConcreteAspect]
  private int M()
  {
    global::System.Console.WriteLine("Override");
    return 0;
  }
}