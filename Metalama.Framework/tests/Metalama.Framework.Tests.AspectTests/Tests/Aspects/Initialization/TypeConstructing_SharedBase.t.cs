[Aspect1]
[Aspect2]
public class TargetCode
{
  public TargetCode()
  {
  }
  static TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect2");
    global::System.Console.WriteLine("TargetCode: Aspect1");
  }
}