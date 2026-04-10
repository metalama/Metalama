[Aspect]
public class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect");
  }
  public TargetCode(int x)
  {
    Console.WriteLine("TargetCode: Aspect");
  }
  private int Method(int a)
  {
    return a;
  }
}