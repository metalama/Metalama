[Aspect1]
[Aspect2]
public class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: Aspect1 1");
    Console.WriteLine("TargetCode: Aspect1 2");
    Console.WriteLine("TargetCode: Aspect2 1");
    Console.WriteLine("TargetCode: Aspect2 2");
  }
  private int Method(int a)
  {
    return a;
  }
}