[Aspect]
public class TargetCode
{
  static TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect first");
  }
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect second");
  }
}