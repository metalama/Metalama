[Aspect1]
[Aspect2]
public class TargetCode
{
  public TargetCode()
  {
  }
  static TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect1 1");
    global::System.Console.WriteLine("TargetCode: Aspect1 2");
    global::System.Console.WriteLine("TargetCode: Aspect2 1");
    global::System.Console.WriteLine("TargetCode: Aspect2 2");
  }
}