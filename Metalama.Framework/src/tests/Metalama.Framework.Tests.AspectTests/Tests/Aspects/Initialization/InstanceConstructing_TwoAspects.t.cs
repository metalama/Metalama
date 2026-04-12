[FirstAspect]
[SecondAspect]
public class TargetCode
{
  public TargetCode()
  {
    Console.WriteLine("TargetCode: FirstAspect First1");
    Console.WriteLine("TargetCode: FirstAspect First2");
    Console.WriteLine("TargetCode: SecondAspect Second1");
    Console.WriteLine("TargetCode: SecondAspect Second2");
  }
  public TargetCode(int x)
  {
    Console.WriteLine("TargetCode: FirstAspect First1");
    Console.WriteLine("TargetCode: FirstAspect First2");
    Console.WriteLine("TargetCode: SecondAspect Second1");
    Console.WriteLine("TargetCode: SecondAspect Second2");
  }
  private int Method(int a)
  {
    return a;
  }
}