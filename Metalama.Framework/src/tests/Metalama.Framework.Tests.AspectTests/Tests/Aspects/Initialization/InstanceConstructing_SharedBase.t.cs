[Aspect1]
[Aspect2]
public class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect1 1");
    global::System.Console.WriteLine("TargetCode: Aspect1 2");
    global::System.Console.WriteLine("TargetCode: Aspect2 1");
    global::System.Console.WriteLine("TargetCode: Aspect2 2");
  }
  private int Method(int a)
  {
    return a;
  }
}