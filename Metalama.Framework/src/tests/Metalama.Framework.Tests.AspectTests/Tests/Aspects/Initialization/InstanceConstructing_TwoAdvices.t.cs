[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect first");
    Console.WriteLine("TargetCode: Aspect second");
  }
  public TargetCode(int x)
  {
    Console.WriteLine("TargetCode: Aspect first");
    Console.WriteLine("TargetCode: Aspect second");
  }
  private int Method(int a)
  {
    return a;
  }
}