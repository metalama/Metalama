[FirstAspect]
[SecondAspect]
public class TargetCode
{
  private int Method(int a)
  {
    return a;
  }
  public TargetCode()
  {
    Console.WriteLine("TargetCode: FirstAspect First1");
    Console.WriteLine("TargetCode: FirstAspect First2");
    Console.WriteLine("TargetCode: SecondAspect Second1");
    Console.WriteLine("TargetCode: SecondAspect Second2");
  }
}