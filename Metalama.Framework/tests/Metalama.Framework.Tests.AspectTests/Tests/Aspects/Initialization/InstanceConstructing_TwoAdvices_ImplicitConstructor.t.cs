[Aspect]
public class TargetCode
{
  private int Method(int a)
  {
    return a;
  }
  public TargetCode()
  {
    global::System.Console.WriteLine("TargetCode: Aspect first");
    global::System.Console.WriteLine("TargetCode: Aspect second");
  }
}