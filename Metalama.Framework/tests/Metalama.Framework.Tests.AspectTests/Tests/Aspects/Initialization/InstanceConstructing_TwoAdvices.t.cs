[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect first");
    global::System.Console.WriteLine("TargetCode: Aspect second");
  }
  public TargetCode(int x)
  {
    global::System.Console.WriteLine("TargetCode: Aspect first");
    global::System.Console.WriteLine("TargetCode: Aspect second");
  }
  private int Method(int a)
  {
    return a;
  }
}