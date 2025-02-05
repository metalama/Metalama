internal class TargetCode
{
  [LogAttribute]
  private int Method(int a)
  {
    global::System.Console.ForegroundColor = global::System.ConsoleColor.Blue;
    return a;
  }
}